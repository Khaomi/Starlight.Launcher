namespace Starlight.Launcher.Models.Settings;

public record AppSettings
{
    public string Theme { get; init; } = "Light";
    public bool AutoSaveSettings { get; init; } = true;
    public int AutoSaveIntervalMs { get; init; } = 500;
}