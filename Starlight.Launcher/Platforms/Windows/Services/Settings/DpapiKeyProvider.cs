using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Starlight.Launcher.Services.Settings;

public sealed class DpapiKeyProvider : ILoginKeyProvider
{
    private readonly ILogger<DpapiKeyProvider> _logger;
    private readonly string _keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starlight.Launcher", "logins.key");
    private static readonly byte[] _entropy = "starlight.logins.v1"u8.ToArray();

    public DpapiKeyProvider(ILogger<DpapiKeyProvider> logger) => _logger = logger;

    public async Task<byte[]> GetOrCreateKeyAsync()
    {
        if (File.Exists(_keyPath))
        {
            var blob = await File.ReadAllBytesAsync(_keyPath);
            var key = ProtectedData.Unprotect(blob, _entropy, DataProtectionScope.CurrentUser);
            _logger.LogInformation("Loaded login key from {path} fp={fp}", _keyPath, Fp(key));
            return key;
        }

        var newKey = RandomNumberGenerator.GetBytes(32);
        var blob2 = ProtectedData.Protect(newKey, _entropy, DataProtectionScope.CurrentUser);
        Directory.CreateDirectory(Path.GetDirectoryName(_keyPath)!);
        var tmp = _keyPath + ".tmp";
        await File.WriteAllBytesAsync(tmp, blob2);
        File.Move(tmp, _keyPath, true);
        _logger.LogWarning("Generated NEW login key at {path} fp={fp}", _keyPath, Fp(newKey));
        return newKey;
    }

    private static string Fp(byte[] key) => Convert.ToHexString(SHA256.HashData(key))[..8];
}
