using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;
using Serilog;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Models.Settings;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public class SettingsService : IAsyncDisposable
{
    #region Variables

    private CancellationTokenSource? _saveCts;


    private AppSettings _settings;
    private readonly SemaphoreSlim _settingsLock = new(1, 1);
    private readonly string _filePath;

    private List<FavoriteServer> _favorites;
    private readonly SemaphoreSlim _favoritesLock = new(1, 1);
    private readonly string _favoritesPath;
    private volatile HashSet<string> _favoriteAddresses = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<Guid, LoginInfo> _logins = new();
    private readonly SemaphoreSlim _loginsLock = new(1, 1);
    private readonly string _loginsPath;

    private readonly ILogger<SettingsService> _logger;

    public event Action? FavoritesChanged;

    public event Action? LoginsChanged;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    #endregion

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
        _favoritesPath = Path.Combine(FileSystem.AppDataDirectory, "favorites.json");
        _loginsPath = Path.Combine(FileSystem.AppDataDirectory, "logins.json");
        _settings = LoadSettings();
        _favorites = LoadFavorites();
        _logins = LoadLogins();
        RebuildFavoritesIndex(); // Rebuild addresses after load.
    }

    public IReadOnlySet<string> GetFavoriteAddressesSnapshot() => _favoriteAddresses;

    public void ScheduleSave(bool favorites = false, bool logins = false)
    {
        var cts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _saveCts, cts);
        old?.Cancel();
        old?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_settings.SaveIntervalMs, cts.Token);
                await SaveAsync(favorites, logins);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-save failed");
            }
        });
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Can't find settings file, fallback to empty.");
                return new();
            }

            var json = File.ReadAllText(_filePath);
            _logger.LogInformation("Successfully loaded settings");
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
            return new();
        }
    }

    private List<FavoriteServer> LoadFavorites()
    {
        try
        {
            if (!File.Exists(_favoritesPath))
            {
                _logger.LogInformation("Can't find favorites file, fallback to empty.");
                return new();
            }

            var json = File.ReadAllText(_favoritesPath);
            _logger.LogInformation("Successfully loaded favorites");
            return JsonSerializer.Deserialize<List<FavoriteServer>>(json) ?? new ();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load favorites, using empty list");
            return new();
        }
    }

    private Dictionary<Guid, LoginInfo> LoadLogins()
    {
        try
        {
            if (!File.Exists(_loginsPath))
            {
                _logger.LogInformation("Can't find logins file, fallback to empty.");
                return new();
            }

            var json = File.ReadAllText(_loginsPath);
            var logins = JsonSerializer.Deserialize<List<LoginInfo>>(json) ?? new();

            _logger.LogInformation("Successfully loaded logins");

            return logins.ToDictionary(x => x.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load logins, using empty list");
            return new();
        }
    }

    public async Task SaveAsync(bool favorites, bool logins = false)
    {
        if (!favorites)
        {
            await _settingsLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_settings, JsonOptions);

                await WriteFileSafeAsync(json, Path.GetDirectoryName(_filePath)!, _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
            finally
            {
                _settingsLock.Release();
            }
        }
        else
        {
            await _favoritesLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_favorites, JsonOptions);

                await WriteFileSafeAsync(json, Path.GetDirectoryName(_favoritesPath)!, _favoritesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save favorites");
            }
            finally
            {
                _favoritesLock.Release();
            }
        }

        if (logins)
        {
            await _loginsLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_logins.Values, JsonOptions);

                await WriteFileSafeAsync(json, Path.GetDirectoryName(_loginsPath)!, _loginsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save logins");
            }
            finally
            {
                _loginsLock.Release();
            }
        }
    }

    private async Task WriteFileSafeAsync(string content, string dir, string filePath)
    {
        Directory.CreateDirectory(dir);

        var tempFile = filePath + ".tmp";
        await File.WriteAllTextAsync(tempFile, content);
        File.Move(tempFile, filePath, true);
    }

    #region Sync Methods

    /// <summary>
    /// Gets the current AppSettings instance under a lock to ensure thread-safe access.
    /// </summary>
    /// <remarks>Acquires _settingsLock before reading and releases it in a finally block so the lock is
    /// always released.</remarks>
    public AppSettings GetSettings()
    {
        _settingsLock.Wait();
        try
        {
            return _settings;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    /// <summary>
    /// Updates the in-memory application settings under a lock and schedules an asynchronous save.
    /// </summary>
    /// <remarks>Acquires an internal lock to ensure thread-safe replacement of the settings. The save is
    /// scheduled for asynchronous persistence and may not occur immediately.</remarks>
    public void WriteSettings(AppSettings settings)
    {
        _settingsLock.Wait();
        try
        {
            _settings = settings;
        }
        finally
        {
            _settingsLock.Release();
        }

        ScheduleSave();
    }

    #endregion

    #region Async Methods

    /// <summary>
    /// Prefer this to use in async methods to avoid races.
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        await _settingsLock.WaitAsync();
        try
        {
            return _settings;
        }
        finally
        {
            _settingsLock.Release();
        }
    }

    /// <summary>
    /// Prefer this to use in async methods to avoid races.
    /// </summary>
    public async Task WriteSettingsAsync(AppSettings settings)
    {
        await _settingsLock.WaitAsync();
        try
        {
            _settings = settings;
        }
        finally
        {
            _settingsLock.Release();
        }

        ScheduleSave();
    }

    #endregion

    #region Favorites sync methods

    public List<FavoriteServer> GetFavorites()
    {
        _favoritesLock.Wait();
        try
        {
            return _favorites;
        }
        finally
        {
            _favoritesLock.Release();
        }
    }

    public void WriteFavorites(List<FavoriteServer> favorites)
    {
        _favoritesLock.Wait();
        try
        {
            _favorites = favorites;
            RebuildFavoritesIndex();
        }
        finally
        {
            _favoritesLock.Release();
        }

        FavoritesChanged?.Invoke();

        ScheduleSave(true);
    }

    #endregion

    #region Favorites async methods

    public async Task<List<FavoriteServer>> GetFavoritesAsync()
    {
        await _favoritesLock.WaitAsync();
        try
        {
            return _favorites;
        }
        finally
        {
            _favoritesLock.Release();
        }
    }

    public async Task WriteFavoritesAsync(List<FavoriteServer> favorites)
    {
        await _favoritesLock.WaitAsync();
        try
        {
            _favorites = favorites;
            RebuildFavoritesIndex();
        }
        finally
        {
            _favoritesLock.Release();
        }

        FavoritesChanged?.Invoke();

        ScheduleSave(true);
    }

    #endregion

    #region Sync Methods Logins

    public Dictionary<Guid, LoginInfo> GetLogins()
    {
        _loginsLock.Wait();
        try
        {
            return _logins;
        }
        finally
        {
            _loginsLock.Release();
        }
    }

    public void WriteLogins(Dictionary<Guid, LoginInfo> logins)
    {
        _loginsLock.Wait();
        try
        {
            _logins = logins;
            RebuildFavoritesIndex();
        }
        finally
        {
            _loginsLock.Release();
        }

        LoginsChanged?.Invoke();

        ScheduleSave(logins: true);
    }

    #endregion

    private void RebuildFavoritesIndex()
    {
        _favoriteAddresses = new HashSet<string>(
            _favorites.Select(f => f.Address),
            StringComparer.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        var cts = Interlocked.Exchange(ref _saveCts, null);
        cts?.Cancel();
        cts?.Dispose();

        await SaveAsync(false, true);
        await SaveAsync(true);

        _settingsLock.Dispose();

        GC.SuppressFinalize(this);
    }

    public async Task CacheFilters(ServerListFilters filters)
    {
        await _settingsLock.WaitAsync();
        try
        {
            _settings.CachedFilters = filters;
        }
        finally
        {
            _settingsLock.Release();
        }

        ScheduleSave();
    }
}