using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Auth;
using Starlight.Launcher.Services.Settings;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;

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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var instance = AppInstance.FindOrRegisterForKey("starlight-main");
        var activated = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (!instance.IsCurrent)
        {
            var done = new ManualResetEventSlim(false);
            _ = Task.Run(async () =>
            {
                try { await instance.RedirectActivationToAsync(activated); }
                finally { done.Set(); }
            });
            done.Wait();
            Process.GetCurrentProcess().Kill();
            return;
        }

        instance.Activated += (_, e) => HandleProtocol(e);
        HandleProtocol(activated);
        base.OnLaunched(args);
    }

    private static void HandleProtocol(AppActivationArguments? e)
    {
        Log.Information("Activation kind: {kind}", e?.Kind);
        if (e?.Kind == ExtendedActivationKind.Protocol && e.Data is IProtocolActivatedEventArgs p)
        {
            Log.Information("Protocol uri: {uri}", p.Uri);
            IPlatformApplication.Current!.Services
                .GetRequiredService<DiscordAuthService>()
                .HandleDeepLink(p.Uri);
        }
        else if (e?.Kind == ExtendedActivationKind.Launch && e.Data is ILaunchActivatedEventArgs l)
        {
            var arg = l.Arguments?.Trim().Trim('"');
            if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) &&
                uri.Scheme.Equals("starlight", StringComparison.OrdinalIgnoreCase))
            {
                IPlatformApplication.Current!.Services
                    .GetRequiredService<DiscordAuthService>()
                    .HandleDeepLink(uri);
            }
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
