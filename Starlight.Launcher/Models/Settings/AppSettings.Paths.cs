using Robust.Launcher.Api.Utility;
using System.Collections.Immutable;

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

    // Client log outputs.
    public string PathClientMacLog => Path.Combine(DirLauncherData, "client.mac.log");
    public string PathClientStdoutLog => Path.Combine(DirLauncherData, "client.stdout.log");
    public string PathClientStderrLog => Path.Combine(DirLauncherData, "client.stderr.log");
    public string DirEngineInstallations => Path.Combine(DirLauncherData, "engines");
    public string DirModuleInstallations => Path.Combine(DirLauncherData, "modules");

    public string PathLoaderSigningKey => Path.Combine(DirLauncherData, "loader_signing_key");

    private const string PrimaryCdnPublicKey = "";

    private const string SecondaryCdnPublicKey = """
        -----BEGIN PUBLIC KEY-----
        MCowBQYDK2VwAyEApQ9mAhMLbmhQqRH7itgNo75S5rCSMsMXvVRmMv1d9NQ=
        -----END PUBLIC KEY-----
        """;

    /// <summary>
    /// Robust build CDNs in priority order. Each entry is one CDN; a CDN may list multiple
    /// mirror URLs treated as interchangeable for availability fallback.
    /// </summary>
    // TODO: Redone to configurable option.
    public static ImmutableArray<RobustCdn> RobustCdns { get; } = [
        // Primary CDN — checked first.
        //new RobustCdn("https://robust-builds.cdn.spacestation14.com/"),

        // Secondary CDN with its own availability fallback (mirror).
        // Checked only if the requested version is missing from the primary CDN's manifest.
        new RobustCdn(
            "https://robust-builds.cdn.spacestation14.com/",
            "https://robust-builds.fallback.cdn.spacestation14.com/") { PublicKey = SecondaryCdnPublicKey },
    ];

    #endregion

    #region Auth

    private static readonly List<string> _defaultAuthServerUrls =
    [
        "https://auth.spacestation14.com/",
        "https://auth.fallback.spacestation14.com/"
    ];

    /// <summary>
    /// Auth servers in priority order. User-editable, any count.
    /// </summary>
    public List<string> AuthServerUrls { get; init; } = [.. _defaultAuthServerUrls];

    public const string FallbackUsername = "JoeGenero";

    /// <summary>
    /// Builds a fresh fallback set from the configured URLs. Falls back to defaults if the user cleared the list.
    /// </summary>
    public UrlFallbackSet BuildAuthUrlSet()
    {
        var urls = AuthServerUrls is { Count: > 0 } list ? list : _defaultAuthServerUrls;
        return new UrlFallbackSet([.. urls]);
    }

    #endregion
}
