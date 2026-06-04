using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Models.Settings;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService : IAsyncDisposable
{
    #region Variables

    private CancellationTokenSource? _settingsSaveCts;
    private CancellationTokenSource? _favoritesSaveCts;
    private CancellationTokenSource? _loginsSaveCts;


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

    public void ScheduleSave(bool settings = true, bool favorites = false, bool logins = false)
    {
        if (settings)
            ScheduleSaveInternal(ref _settingsSaveCts, SaveSettingsAsync, "settings");

        if (favorites)
            ScheduleSaveInternal(ref _favoritesSaveCts, SaveFavoritesAsync, "favorites");

        if (logins)
            ScheduleSaveInternal(ref _loginsSaveCts, SaveLoginsAsync, "logins");
    }

    private void ScheduleSaveInternal(
        ref CancellationTokenSource? ctsField,
        Func<Task> saveAction,
        string what)
    {
        var cts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref ctsField, cts);
        old?.Cancel();
        old?.Dispose();

        var delay = _settings.SaveIntervalMs;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                await saveAction();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-save failed for {what}", what);
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

    private async Task SaveSettingsAsync()
    {
        await _settingsLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            await WriteFileSafeAsync(json, Path.GetDirectoryName(_filePath)!, _filePath);
            _logger.LogDebug("Settings saved");
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

    private async Task SaveFavoritesAsync()
    {
        await _favoritesLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_favorites, JsonOptions);
            await WriteFileSafeAsync(json, Path.GetDirectoryName(_favoritesPath)!, _favoritesPath);
            _logger.LogDebug("Favorites saved");
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

    private async Task SaveLoginsAsync()
    {
        await _loginsLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_logins.Values, JsonOptions);
            await WriteFileSafeAsync(json, Path.GetDirectoryName(_loginsPath)!, _loginsPath);
            _logger.LogDebug("Logins saved ({count})", _logins.Count);
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

    public async Task SaveAllAsync(bool settings = true, bool favorites = true, bool logins = true)
    {
        var tasks = new List<Task>();
        if (settings) tasks.Add(SaveSettingsAsync());
        if (favorites) tasks.Add(SaveFavoritesAsync());
        if (logins) tasks.Add(SaveLoginsAsync());
        await Task.WhenAll(tasks);
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

        ScheduleSave(settings: true);
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

        ScheduleSave(settings: true);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _settingsSaveCts, null)?.Cancel();
        Interlocked.Exchange(ref _favoritesSaveCts, null)?.Cancel();
        Interlocked.Exchange(ref _loginsSaveCts, null)?.Cancel();

        await SaveAllAsync();

        _settingsLock.Dispose();
        _favoritesLock.Dispose();
        _loginsLock.Dispose();

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

        ScheduleSave(settings: true);
    }
}