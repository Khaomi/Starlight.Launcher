using System.Runtime.Versioning;
using Starlight.Launcher.Services.Auth;

namespace Starlight.Launcher;

public partial class App : Application
{
    public App() => InitializeComponent();

    [SupportedOSPlatform("windows10.0.17763.0")]
    protected override Window CreateWindow(IActivationState? activationState) => new(new MainPage()) { Title = "Starlight.Launcher" };

    [SupportedOSPlatform("windows10.0.17763.0")]
    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        IPlatformApplication.Current!.Services
            .GetRequiredService<DiscordAuthService>()
            .HandleDeepLink(uri);
    }
}
