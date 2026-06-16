using System.Runtime.InteropServices;
using Microsoft.Maui.Platform;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Auth;
using Starlight.Launcher.Services.Settings;
using Windows.ApplicationModel.Activation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starlight.Launcher.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    private const string ProtocolScheme = "starlight";

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

    private static void RegisterProtocol()
    {
        try
        {
            var exe = Environment.ProcessPath!;

            ActivationRegistrationManager.RegisterForProtocolActivation(
                scheme: ProtocolScheme,
                logo: $"{exe},0",
                displayName: "Starlight Protocol",
                exePath: exe);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to register starlight protocol");
        }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        RegisterProtocol();

        var instance = AppInstance.FindOrRegisterForKey("starlight-main");
        var activated = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (!instance.IsCurrent)
        {
            await instance.RedirectActivationToAsync(activated);
            return;
        }

        instance.Activated += (_, e) => HandleProtocol(e, warm: true);

        HandleProtocol(activated, warm: false);

        base.OnLaunched(args);
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private static string[] SplitCommandLine(string commandLine)
    {
        var ptr = CommandLineToArgvW(commandLine, out var argc);
        if (ptr == IntPtr.Zero) return Array.Empty<string>();
        try
        {
            var args = new string[argc];
            for (var i = 0; i < argc; i++)
            {
                var p = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
                args[i] = Marshal.PtrToStringUni(p) ?? "";
            }
            return args;
        }
        finally
        {
            LocalFree(ptr);
        }
    }

    private static string[]? ExtractCommands(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var argv = SplitCommandLine(raw);
        var idx = Array.IndexOf(argv, "--commands");
        if (idx < 0 || idx + 1 >= argv.Length) return null;

        return argv[(idx + 1)..];
    }

    private static void HandleProtocol(AppActivationArguments? e, bool warm)
    {
        var services = IPlatformApplication.Current?.Services;
        if (services is null) return;

        switch (e?.Kind)
        {
            case ExtendedActivationKind.Protocol when e.Data is IProtocolActivatedEventArgs p:
                {
                    Log.Information("Protocol URI: {uri}", p.Uri);

                    services.GetRequiredService<DiscordAuthService>()
                        .HandleDeepLink(p.Uri);

                    break;
                }

            case ExtendedActivationKind.Launch:
                {
                    var launch = (ILaunchActivatedEventArgs?)e.Data;
                    var raw = launch?.Arguments;

                    var uri = ExtractStarlightUri(raw);
                    if (uri != null)
                    {
                        services.GetRequiredService<DiscordAuthService>()
                            .HandleDeepLink(uri);
                        return;
                    }

                    if (warm)
                    {
                        var commands = ExtractCommands(raw);
                        if (commands is { Length: > 0 })
                        {
                            var lc = services.GetRequiredService<LauncherCommands>();
                            foreach (var c in commands)
                                _ = lc.QueueCommand(c);
                        }
                    }

                    break;
                }
        }
    }

    private static Uri? ExtractStarlightUri(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var args = SplitCommandLine(raw);

        foreach (var arg in args)
        {
            if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) &&
                uri.Scheme.Equals(ProtocolScheme, StringComparison.OrdinalIgnoreCase))
            {
                return uri;
            }
        }

        return null;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
