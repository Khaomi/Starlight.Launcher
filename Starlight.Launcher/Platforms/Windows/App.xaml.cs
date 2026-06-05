using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starlight.Launcher.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
            var nativeWindow = handler.PlatformView;
            var appWindow = nativeWindow.GetAppWindow();

            nativeWindow.ExtendsContentIntoTitleBar = true;
            nativeWindow.SetTitleBar(null);

            var settings = handler.MauiContext?.Services.GetRequiredService<SettingsService>();
            var tray = handler.MauiContext?.Services.GetRequiredService<INativeTray>();

            if (appWindow is not null)
            {
                appWindow.Closing += (sender, args) =>
                {
                    if (settings?.GetSettings().CollapseInTrayOnClose == true)
                    {
                        args.Cancel = true;
                        tray?.HideWindow();
                    }
                };

                appWindow.Changed += (sender, args) =>
                {
                    if (sender.Presenter is OverlappedPresenter presenter)
                    {
                        if (presenter.State == OverlappedPresenterState.Minimized)
                        {
                            if (settings?.GetSettings().CollapseInTrayOnMinimize == true)
                            {
                                tray?.HideWindow();
                            }
                        }
                    }
                };
            }
        });
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
