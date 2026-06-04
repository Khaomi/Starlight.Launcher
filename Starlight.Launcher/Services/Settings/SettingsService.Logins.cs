using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;
using System.Text.Json;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
    public Dictionary<Guid, LoginInfo> GetLogins()
    {
        _loginsLock.Wait();
        try
        {
            return new Dictionary<Guid, LoginInfo>(_logins);
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
            return new Dictionary<Guid, LoginInfo>(_logins);
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
        }
        finally
        {
            _loginsLock.Release();
        }

        LoginsChanged?.Invoke();

        ScheduleSave(settings: false, logins: true);
    }
}
