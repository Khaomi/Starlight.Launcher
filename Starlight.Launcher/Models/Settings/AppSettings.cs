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
}