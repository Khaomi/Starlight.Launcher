using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
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

    public void AddLogin(LoginInfo login)
    {
        _loginsLock.Wait();
        try
        {
            _logins[login.UserId] = login;
        }
        finally
        {
            _loginsLock.Release();
        }

        LoginsChanged?.Invoke();

        ScheduleSave(settings: false, logins: true);
    }

    public void WriteLogins(Dictionary<Guid, LoginInfo> logins)
    {
        _loginsLock.Wait();
        try
        {
            _logins = logins;
        }
        finally
        {
            _loginsLock.Release();
        }

        LoginsChanged?.Invoke();

        ScheduleSave(settings: false, logins: true);
    }

    public async Task<Dictionary<Guid, LoginInfo>> GetLoginsAsync()
    {
        await _loginsLock.WaitAsync();
        try
        {
            return _logins;
        }
        finally
        {
            _loginsLock.Release();
        }
    }

    public async Task WriteLoginsAsync(Dictionary<Guid, LoginInfo> logins)
    {
        await _loginsLock.WaitAsync();
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
}
