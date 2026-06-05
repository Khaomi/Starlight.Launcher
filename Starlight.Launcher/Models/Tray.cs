namespace Starlight.Launcher.Models;

public sealed record TrayMenuItem(string Text, Action? Invoke = null, bool IsSeparator = false)
{
    public static TrayMenuItem Separator => new(string.Empty, IsSeparator: true);
}

public sealed record TrayOptions(string Tooltip, string IconPath);