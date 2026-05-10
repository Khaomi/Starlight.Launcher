namespace Starlight.Launcher.Models.Settings;

public record AppSettings
{
    #region Appearance
    /// <summary>
    /// App theme
    /// </summary>
    public AppTheme Theme { get; init; } = AppTheme.Dark;

    /// <summary>
    /// Determines should we place navigation menu at the bottom of app or at the left side
    /// </summary>
    public bool BottomNavigation { get; init; } = true;

    /// <summary>
    /// Determines should we place search bar at the bottom of TOOLBAR or at the top
    /// </summary>
    public bool ServerListToolbarBottomSearch { get; init; } = false;

    /// <summary>
    /// Determines should we place search bar at the bottom of APP or at the top
    /// </summary>
    public bool ServerListToolBarBottomSearchBar { get; init ; } = false;

    /// <summary>
    /// Determines should we place TAGS bar at the bottom of APP or at the top
    /// </summary>
    public bool ServerListToolBarBottomTagsBar { get; init; } = true;
    #endregion
    /// <summary>
    /// AutoSave settings?
    /// </summary>
    public bool AutoSaveSettings { get; init; } = true;
    /// <summary>
    /// AutoSave interval in milliseconds
    /// </summary>
    public int AutoSaveIntervalMs { get; init; } = 500;
    /// <summary>
    /// A list of hub urls to use for server lists
    /// </summary>
    public List<Hub> Hubs { get; init; } = [ new Hub() { HubUri = new Uri("https://hub.spacestation14.com/"), Priority = 0} ];
}