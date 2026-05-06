namespace Starlight.Launcher.Models.Settings;

public record AppSettings
{
    /// <summary>
    /// App theme
    /// </summary>
    public AppTheme Theme { get; init; } = AppTheme.Dark;
    /// <summary>
    /// AutoSave settings?
    /// </summary>
    public bool AutoSaveSettings { get; init; } = true;
    /// <summary>
    /// AutoSave interval in milliseconds
    /// </summary>
    public int AutoSaveIntervalMs { get; init; } = 500;
    /// <summary>
    /// Determines should we place navigation menu at the bottom of app or at the left side
    /// </summary>
    public bool BottomNavigation { get; init; } = true;
    /// <summary>
    /// A list of hub urls to use for server lists
    /// </summary>
    public List<Hub> Hubs { get; init; } = [ new Hub() { HubUri = new Uri("https://hub.spacestation14.com/"), Priority = 0} ];
}