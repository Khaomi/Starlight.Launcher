using System.Runtime.InteropServices;

namespace Starlight.Launcher.Services;

public partial class LauncherCommands
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    partial void ActivateWindowPlatform(Window window)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
            return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        if (hwnd == IntPtr.Zero)
            return;

        ShowWindow(hwnd, SW_RESTORE);
        SetForegroundWindow(hwnd);
    }
}
