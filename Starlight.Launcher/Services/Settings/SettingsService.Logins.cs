using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Robust.Launcher.Api.Models.Data;

namespace Starlight.Launcher.Services.Settings;

public sealed partial class SettingsService
{
    private volatile bool _loginsLoaded;
    private volatile bool _loginsLoadFailed;

    public async Task InitializeLoginsAsync()
    {
        var loaded = await LoadLoginsAsync();
        await _loginsLock.WaitAsync();
        try { _logins = loaded; }
        finally { _loginsLock.Release(); }
        _loginsLoaded = true;
    }

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

    public void UpdateLogin(LoginInfo login)
    {
        _loginsLock.Wait();
        try
        {
            if (_logins.ContainsKey(login.UserId))
            {
                _logins[login.UserId] = login;
            }
            else
            {
                _logins.Add(login.UserId, login);
            }
        }
        finally
        {
            _loginsLock.Release();
        }

        LoginsChanged?.Invoke();

        ScheduleSaveInternal(ref _loginsSaveCts, SaveLoginsEncryptedAsync, "logins");
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

        ScheduleSaveInternal(ref _loginsSaveCts, SaveLoginsEncryptedAsync, "logins");
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

        ScheduleSaveInternal(ref _loginsSaveCts, SaveLoginsEncryptedAsync, "logins");
    }

    private static async Task WriteBytesSafeAsync(byte[] content, string dir, string filePath)
    {
        Directory.CreateDirectory(dir);
        var tempFile = filePath + ".tmp";
        await File.WriteAllBytesAsync(tempFile, content);
        File.Move(tempFile, filePath, true);
    }

    private async Task SaveLoginsEncryptedAsync()
    {
        if (!_loginsLoaded)
        {
            _logger.LogWarning("Skipped logins save before load completed");
            return;
        }
        if (_loginsLoadFailed)
        {
            _logger.LogWarning("Skipped logins save: last load failed, refusing to overwrite existing file");
            return;
        }

        await _loginsLock.WaitAsync();
        try { await WriteLoginsCoreAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to save encrypted logins"); }
        finally { _loginsLock.Release(); }
    }

    private async Task WriteLoginsCoreAsync()
    {
        var json = JsonSerializer.Serialize(_logins.Values, _jsonOptions);
        var encrypted = await EncryptAsync(Encoding.UTF8.GetBytes(json));
        await WriteBytesSafeAsync(encrypted, Path.GetDirectoryName(_loginsPath)!, _loginsPath);
    }

    private async Task<Dictionary<Guid, LoginInfo>> LoadLoginsAsync()
    {
        if (!File.Exists(_loginsPath))
            return new();

        var raw = await File.ReadAllBytesAsync(_loginsPath);

        if (LooksEncrypted(raw))
        {
            try
            {
                var json = Encoding.UTF8.GetString(await DecryptAsync(raw));
                var list = JsonSerializer.Deserialize<List<LoginInfo>>(json) ?? new();
                return list.ToDictionary(x => x.UserId);
            }
            catch (CryptographicException ex)
            {
                _loginsLoadFailed = true;
                _logger.LogError(ex, "Cannot decrypt logins; keeping file intact, saving disabled");
                return new();
            }
        }

        try
        {
            var json = Encoding.UTF8.GetString(raw);
            var list = JsonSerializer.Deserialize<List<LoginInfo>>(json) ?? new();
            _logins = list.ToDictionary(x => x.UserId);
            await _loginsLock.WaitAsync();
            try { await WriteLoginsCoreAsync(); }
            finally { _loginsLock.Release(); }
            return _logins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load logins; starting empty");
            return new();
        }
    }
}
