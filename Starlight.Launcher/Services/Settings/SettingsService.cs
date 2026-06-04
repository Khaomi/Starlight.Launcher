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
    private CancellationTokenSource? _enginesSaveCts;
    private CancellationTokenSource? _modulesSaveCts;


    private AppSettings _settings;
    private readonly SemaphoreSlim _settingsLock = new(1, 1);
    private readonly string _filePath;

    private List<FavoriteServer> _favorites;
    private readonly SemaphoreSlim _favoritesLock = new(1, 1);
    private readonly string _favoritesPath;
    private volatile HashSet<string> _favoriteAddresses = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<Guid, LoginInfo> _logins;
    private readonly SemaphoreSlim _loginsLock = new(1, 1);
    private readonly string _loginsPath;

    // Version to engine version info(signature)
    private Dictionary<string, InstalledEngineVersion> _engineInstallations;
    private readonly SemaphoreSlim _enginesLock = new(1, 1);
    private readonly string _enginesPath;

    private HashSet<(string Version, string Name)> _engineModules;
    private readonly SemaphoreSlim _modulesLock = new(1, 1);
    private readonly string _modulesPath;

    private readonly ILogger<SettingsService> _logger;

    public event Action? FavoritesChanged;

    public event Action? LoginsChanged;

    public event Action? EnginesChanged;

    public event Action? ModulesChanged;

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
        _enginesPath = Path.Combine(FileSystem.AppDataDirectory, "engines.json");
        _modulesPath = Path.Combine(FileSystem.AppDataDirectory, "modules.json");
        _settings = LoadJson(_filePath, new AppSettings());
        _favorites = LoadJson(_favoritesPath, new List<FavoriteServer>());
        _logins = LoadJson(_loginsPath, new List<LoginInfo>()).ToDictionary(x => x.UserId);
        _engineInstallations = LoadJson(_enginesPath, new List<InstalledEngineVersion>()).ToDictionary(x => x.Version);
        _engineModules = LoadJson(_modulesPath, new HashSet<(string Version, string Name)>());
        RebuildFavoritesIndex(); // Rebuild addresses after load.
    }

    public IReadOnlySet<string> GetFavoriteAddressesSnapshot() => _favoriteAddresses;

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

    private async Task SaveJsonAsync<T>(string path, SemaphoreSlim slim, T obj)
    {
        await slim.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(obj, JsonOptions);
            await WriteFileSafeAsync(json, Path.GetDirectoryName(path)!, path);
#if DEBUG
            _logger.LogDebug("{0} saved", path);
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {0}", path);
        }
        finally
        {
            slim.Release();
        }
    }

    public async Task SaveAllAsync()
    {
        var tasks = new List<Task>
        {
            SaveJsonAsync(_filePath, _settingsLock, _settings),
            SaveJsonAsync(_favoritesPath, _favoritesLock, _favorites),
            SaveJsonAsync(_loginsPath, _loginsLock, _logins.Values),
            SaveJsonAsync(_enginesPath, _enginesLock, _engineInstallations.Values),
            SaveJsonAsync(_modulesPath, _modulesLock, _engineModules)
        };
        await Task.WhenAll(tasks);
    }

    private static async Task WriteFileSafeAsync(string content, string dir, string filePath)
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

        ScheduleSaveInternal(ref _settingsSaveCts, () => SaveJsonAsync(_filePath, _settingsLock, _settings), "settings");
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

        ScheduleSaveInternal(ref _settingsSaveCts, () => SaveJsonAsync(_filePath, _settingsLock, _settings), "settings");
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

        ScheduleSaveInternal(ref _settingsSaveCts, () => SaveJsonAsync(_filePath, _settingsLock, _settings), "settings");
    }

    private T LoadJson<T>(string path, T fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogInformation("Can't find {0} file, fallback to empty.", path);
                return fallback;
            }

            var json = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<T>(json) ?? fallback;

            _logger.LogInformation("Successfully loaded {0}", path);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load {0}, using empty list", path);
            return fallback;
        }
    }
}