namespace Starlight.Launcher.Models.Settings;

public record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.Dark;
    public bool AutoSaveSettings { get; init; } = true;
    public int AutoSaveIntervalMs { get; init; } = 500;
}