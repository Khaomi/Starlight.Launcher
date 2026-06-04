using Robust.Launcher.Api.Utility;

namespace Starlight.Launcher.Models.Settings;

public partial record AppSettings
{
    #region Paths

    // Base directory for all launcher data. This is currently the one "real" hardcoded path.
    public string DirLauncherData { get; init; } = FileSystem.AppDataDirectory;

    // Where the launcher itself is installed. Used to locate the loader/engine (release builds).
    public string DirLauncherInstall { get; init; } = AppContext.BaseDirectory;

    public string DirLogs => Path.Combine(DirLauncherData, "logs");

    // SQLite content DB the loader reads versions/blobs from.
    // IMPORTANT: this MUST point at the same file that ContentManager.GetSqliteConnection() uses,
    // otherwise the loader won't find the version the Updater just wrote.
    public string PathContentDb => Path.Combine(DirLauncherData, "content.db");

    // Public key used to verify engine signatures (loader-side, currently unused / passing disabled below).
    public string PathPublicKey => Path.Combine(DirLauncherData, "signing_key");

    // Client log outputs.
    public string PathClientMacLog => Path.Combine(DirLauncherData, "client.mac.log");
    public string PathClientStdoutLog => Path.Combine(DirLauncherData, "client.stdout.log");
    public string PathClientStderrLog => Path.Combine(DirLauncherData, "client.stderr.log");
    // Auth server URL passed to the client for multiplayer auth.
    // (Original launcher used ConfigConstants.AuthUrl.GetMostSuccessfulUrl().)
    public UrlFallbackSet AuthServerUrl { get; init; } = new(["https://auth.spacestation14.com/", "https://auth.fallback.spacestation14.com/"]);

    // Username used when no account is logged in.
    public const string FallbackUsername = "JoeGenero";

    #endregion
}
