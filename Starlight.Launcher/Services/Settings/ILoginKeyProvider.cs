namespace Starlight.Launcher.Services.Settings;

public interface ILoginKeyProvider
{
    Task<byte[]> GetOrCreateKeyAsync();
}
