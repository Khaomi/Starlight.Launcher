using UIKit;

namespace Starlight.Launcher.Services;

public partial class LauncherCommands
{
    partial void ActivateWindowPlatform(Window window)
    {
        if (window.Handler?.PlatformView is UIWindow uiWindow)
            uiWindow.MakeKeyAndVisible();
    }
}
