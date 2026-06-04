using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
    public Dictionary<string, InstalledEngineVersion> GetEngines()
    {
        _enginesLock.Wait();
        try
        {
            return new Dictionary<string, InstalledEngineVersion>(_engineInstallations);
        }
        finally
        {
            _enginesLock.Release();
        }
    }

    public void AddInstalledEngine(InstalledEngineVersion version)
    {
        _enginesLock.Wait();
        try
        {
            _engineInstallations[version.Version] = version;
        }
        finally
        {
            _enginesLock.Release();
        }

        EnginesChanged?.Invoke();

        ScheduleSave(settings: false, engines: true);
    }

    public void RemoveInstalledEngine(string version)
    {
        _enginesLock.Wait();
        try
        {
            _engineInstallations.Remove(version);
        }
        finally
        {
            _enginesLock.Release();
        }

        EnginesChanged?.Invoke();

        ScheduleSave(settings: false, engines: true);
    }

    public void WriteEngines(Dictionary<string, InstalledEngineVersion> engines)
    {
        _enginesLock.Wait();
        try
        {
            _engineInstallations = engines;
        }
        finally
        {
            _enginesLock.Release();
        }

        EnginesChanged?.Invoke();

        ScheduleSave(settings: false, engines: true);
    }

    public async Task<Dictionary<string, InstalledEngineVersion>> GetEnginesAsync()
    {
        await _enginesLock.WaitAsync();
        try
        {
            return new Dictionary<string, InstalledEngineVersion>(_engineInstallations);
        }
        finally
        {
            _enginesLock.Release();
        }
    }

    public async Task WriteEnginesAsync(Dictionary<string, InstalledEngineVersion> engines)
    {
        await _enginesLock.WaitAsync();
        try
        {
            _engineInstallations = engines;
        }
        finally
        {
            _enginesLock.Release();
        }

        EnginesChanged?.Invoke();

        ScheduleSave(settings: false, engines: true);
    }
}
