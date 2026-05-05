using Microsoft.Extensions.Logging;
using Serilog;
using Starlight.Launcher.Models.Settings;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public class SettingsService : IAsyncDisposable
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _saveCts;
    private AppSettings _settings;

    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
        _settings = Load();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void ScheduleSave()
    {
        if (!_settings.AutoSaveSettings)
            return;

        var cts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref _saveCts, cts);
        old?.Cancel();
        old?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_settings.AutoSaveIntervalMs, cts.Token);
                await SaveAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-save failed");
            }
        });
    }

    private AppSettings Load()
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

    public async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);

            await WriteFileSafeAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteFileSafeAsync(string content)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        var tempFile = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempFile, content);
        File.Move(tempFile, _filePath, true);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _settings;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteSettingsAsync(AppSettings settings)
    {
        await _lock.WaitAsync();
        try
        {
            _settings = settings;
        }
        finally
        {
            _lock.Release();
        }

        ScheduleSave();
    }

    public async ValueTask DisposeAsync()
    {
        var cts = Interlocked.Exchange(ref _saveCts, null);
        cts?.Cancel();
        cts?.Dispose();

        await SaveAsync();

        _lock.Dispose();

        GC.SuppressFinalize(this);
    }
}