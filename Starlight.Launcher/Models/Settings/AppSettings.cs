using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Models.ServerStatus;

namespace Starlight.Launcher.Models.Settings;

public partial record AppSettings
{
    #region Appearance
    /// <summary>
    /// App theme
    /// </summary>
    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>
    /// Determines should we place navigation menu at the bottom of app or at the left side
    /// </summary>
    public ElementPosition Navigation { get; init; } = ElementPosition.Bottom;

    /// <summary>
    /// Determines should we place search bar at the bottom of TOOLBAR or at the top
    /// </summary>
    public bool ServerListToolbarBottomSearch { get; init; } = false;

    /// <summary>
    /// Determines should we place search bar at the bottom of APP or at the top
    /// </summary>
    public ElementPosition ServerListToolBarSearchPosition { get; init ; } = ElementPosition.Top;

    /// <summary>
    /// Determines should we place TAGS bar at the bottom of APP or at the top
    /// </summary>
    public ElementPosition ServerListToolBarBottomTagsPosition { get; init; } = ElementPosition.Left;
    /// <summary>
    /// Determines should we open TAGS bar by default or it should be closed by default
    /// </summary>
    public bool ServerListToolBarTagsBarOpen { get; init; } = true;
    #endregion
    /// <summary>
    /// Save interval in milliseconds
    /// </summary>
    public int SaveIntervalMs { get; init; } = 500;
    /// <summary>
    /// A list of hub urls to use for server lists
    /// </summary>
    public List<Hub> Hubs { get; init; } = [ new Hub() { HubUri = new Uri("https://hub.spacestation14.com/"), Priority = 0} ];
    /// <summary>
    /// Currently selected language. Should be a key from LocalizationsIndex. Default is "en-US"
    /// </summary>
    public string? SelectedLanguage { get; init; } = null;
    /// <summary>
    /// Currently selected login.
    /// </summary>
    public Guid? SelectedLoginId { get; set; } = null;

    #region Cache
    public ServerListFilters CachedFilters { get; set; } = new ServerListFilters();
    #endregion

    #region Game / launch
    /// <summary>
    /// Force render compatibility mode (GLES2).
    /// </summary>
    public bool CompatMode { get; init; } = false;

    /// <summary>
    /// Disable engine signature verification. For debugging/development only.
    /// </summary>
    public bool DisableSigning { get; init; } = false;

    /// <summary>
    /// Enable local overriding of engine versions.
    /// </summary>
    /// <remarks>
    /// If enabled and on a development build,
    /// the launcher will pull all engine versions and modules from <see cref="EngineOverridePath"/>.
    /// This can be set to <c>RobustToolbox/release/</c> to instantly pull in packaged engine builds.
    /// </remarks>
    public bool EngineOverrideEnabled { get; init; } = false;

    public string EngineOverridePath { get; init; } = "";

    public TimeSpan RobustManifestCacheTime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum amount of TOTAL versions to keep in the content database.
    /// </summary>
    public int MaxVersionsToKeep { get; init; } = 15;

    /// <summary>
    /// Maximum amount of versions to keep of a specific fork ID.
    /// </summary>
    public int MaxForkVersionsToKeep { get; init; } = 3;

    /// <summary>
    /// If a download gets interrupted, keep the files for a week.
    /// </summary>
    public int InterruptibleDownloadKeepHours = 7 * 24;

    #endregion

    #region Privacy policies
    /// <summary>
    ///Privacy policies accepted by the user, the key is the policy identifier.
    /// </summary>
    public Dictionary<string, AcceptedPrivacyPolicy> AcceptedPrivacyPolicies { get; init; } = new();
    #endregion
}