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
        RegisterProtocol();

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

    private static void RegisterProtocol()
    {
        try
        {
            var exe = Environment.ProcessPath!;
            ActivationRegistrationManager.RegisterForProtocolActivation(
                scheme: "starlight",
                logo: $"{exe},0",
                displayName: "Starlight Protocol",
                exePath: exe);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to register starlight protocol");
        }
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
        if (e?.Kind == ExtendedActivationKind.Protocol && e.Data is IProtocolActivatedEventArgs p)
        {
            Log.Information("Protocol uri: {uri}", p.Uri);
            IPlatformApplication.Current!.Services
                .GetRequiredService<DiscordAuthService>()
                .HandleDeepLink(p.Uri);
        }
        else if (e?.Kind == ExtendedActivationKind.Launch)
        {
            Log.Information("Launch data type: {t}", e.Data?.GetType().FullName);
            var raw = (e.Data as ILaunchActivatedEventArgs)?.Arguments;
            Log.Information("Launch args: {raw}", raw);

            var uri = ExtractStarlightUri(raw);
            if (uri is not null)
                IPlatformApplication.Current!.Services
                    .GetRequiredService<DiscordAuthService>()
                    .HandleDeepLink(uri);
        }
    }

    private static Uri? ExtractStarlightUri(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var idx = raw.IndexOf("starlight://", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var part = raw[idx..].Trim().Trim('"');
        return Uri.TryCreate(part, UriKind.Absolute, out var u) ? u : null;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
