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
    public string DirEngineInstallations => Path.Combine(DirLauncherData, "engines");
    public string DirModuleInstallations => Path.Combine(DirLauncherData, "modules");

    // TODO: Redone to configurable option.
    private static readonly UrlFallbackSet RobustBuildsBaseUrl = new([
        "https://robust-builds.cdn.spacestation14.com/",
        "https://robust-builds.fallback.cdn.spacestation14.com/"
    ]);

    public UrlFallbackSet RobustBuildsManifest => RobustBuildsBaseUrl + "manifest.json";
    public UrlFallbackSet RobustModulesManifest => RobustBuildsBaseUrl + "modules.json";

    #endregion

    #region Auth

    private static readonly List<string> DefaultAuthServerUrls =
    [
        "https://auth.spacestation14.com/",
        "https://auth.fallback.spacestation14.com/"
    ];

    /// <summary>
    /// Auth servers in priority order. User-editable, any count.
    /// </summary>
    public List<string> AuthServerUrls { get; init; } = [.. DefaultAuthServerUrls];

    public const string FallbackUsername = "JoeGenero";

    /// <summary>
    /// Builds a fresh fallback set from the configured URLs. Falls back to defaults if the user cleared the list.
    /// </summary>
    public UrlFallbackSet BuildAuthUrlSet()
    {
        var urls = AuthServerUrls is { Count: > 0 } list ? list : DefaultAuthServerUrls;
        return new UrlFallbackSet([.. urls]);
    }

    #endregion
}
