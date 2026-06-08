using Starlight.Launcher.Models;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Services;

public interface INativeTray : IDisposable
{
    void Initialize(TrayOptions options, IReadOnlyList<TrayMenuItem> menu);
    void ShowWindow();
    void HideWindow();
    void UpdateTooltip(string text);
    bool IsWindowVisible { get; }
    event EventHandler? IconActivated;
}

public sealed class TrayCoordinator
{
    private readonly INativeTray _tray;

    public TrayCoordinator(INativeTray tray) => _tray = tray;

    public void Initialize()
    {
        var menu = new List<TrayMenuItem>
        {
            new("Open", () => _tray.ShowWindow()),
            TrayMenuItem.Separator,
            new("Quit", QuitApp),
        };
        _tray.Initialize(new TrayOptions("STARLIGHT.LAUNCHER", "Resources/AppIcon/icon.ico"), menu);
        _tray.IconActivated += (_, _) => _tray.ShowWindow();
    }

    private void QuitApp() => Application.Current?.Quit();
}
