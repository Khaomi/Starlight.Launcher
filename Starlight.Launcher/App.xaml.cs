using System.Runtime.Versioning;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Auth;
using Starlight.Launcher.Services.State;
using TerraFX.Interop.Windows;

namespace Starlight.Launcher;

public partial class App : Application
{
    private readonly LauncherCommands _commands;
    private readonly LauncherMessaging _messaging;
    private readonly AppState _state;
    public App(LauncherCommands commands, LauncherMessaging messaging, AppState state)
    {
        InitializeComponent();

        _commands = commands;
        _messaging = messaging;
        _state = state;

        _commands.RunCommandTask();
        _messaging.StartServerTask(commands);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage(_state)) { Title = "Starlight.Launcher" };

        window.Destroying += (_, _) =>
        {
            _commands.Shutdown();
            _messaging.StopAndWait();
        };
        window.Stopped += (s, e) => _state.NotifyPaused();
        window.Resumed += (s, e) => _state.NotifyResumed();

        return window;
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        IPlatformApplication.Current!.Services
            .GetRequiredService<DiscordAuthService>()
            .HandleDeepLink(uri);
    }
}
