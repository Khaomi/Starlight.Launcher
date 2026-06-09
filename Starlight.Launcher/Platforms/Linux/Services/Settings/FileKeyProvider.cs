/*
using System.Security.Cryptography;

public sealed class FileKeyProvider : ILoginKeyProvider
{
    private readonly string _keyPath = Path.Combine(FileSystem.AppDataDirectory, "logins.key");

    public async Task<byte[]> GetOrCreateKeyAsync()
    {
        if (File.Exists(_keyPath))
            return Convert.FromBase64String(await File.ReadAllTextAsync(_keyPath));

        var key = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(Path.GetDirectoryName(_keyPath)!);

        var tmp = _keyPath + ".tmp";
        await File.WriteAllTextAsync(tmp, Convert.ToBase64String(key));
        if (OperatingSystem.IsLinux())
            File.SetUnixFileMode(tmp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(tmp, _keyPath, true);
        return key;
    }
}
*/
