using Starlight.Launcher.Services.Auth;

namespace Starlight.Launcher;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "Starlight.Launcher" };
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        IPlatformApplication.Current!.Services
            .GetRequiredService<DiscordAuthService>()
            .HandleDeepLink(uri);
    }
}
