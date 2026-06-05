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
    private readonly SettingsService _settings;

    public TrayCoordinator(INativeTray tray, SettingsService settings)
    {
        _tray = tray;
        _settings = settings;
    }

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

        if (_settings.GetSettings().CollapseInTrayOnStart)
            _tray.HideWindow();
    }

    private void QuitApp() => Application.Current?.Quit();
}