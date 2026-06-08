using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Media.Imaging;
using Starlight.Launcher.Models;

namespace Starlight.Launcher.Services;

[SupportedOSPlatform("windows5.1.2600")]
public sealed partial class WindowsTray : INativeTray
{
    private TaskbarIcon? _icon;
    private MauiWinUIWindow? _win => Application.Current!.Windows[0].Handler!.PlatformView! as MauiWinUIWindow;

    public event EventHandler? IconActivated;
    public bool IsWindowVisible => _win?.AppWindow.IsVisible == true;

    public void Initialize(TrayOptions o, IReadOnlyList<TrayMenuItem> menu)
    {
        var flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();
        foreach (var m in menu)
        {
            if (m.IsSeparator) { flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator()); continue; }
            var mi = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = m.Text };
            mi.Click += (_, _) => m.Invoke?.Invoke();
            flyout.Items.Add(mi);
        }

        _icon = new TaskbarIcon
        {
            ToolTipText = o.Tooltip,
            IconSource = new BitmapImage(new Uri($"ms-appx:///{o.IconPath}")),
            ContextFlyout = flyout,
            LeftClickCommand = new RelayCommand(() => IconActivated?.Invoke(this, EventArgs.Empty))
        };
        _icon.ForceCreate();
    }

    public void ShowWindow() { _win?.AppWindow.Show(); _win?.Activate(); }
    public void HideWindow() => _win?.AppWindow.Hide();
    public void UpdateTooltip(string t) => _icon?.ToolTipText = t;
    public void Dispose() => _icon?.Dispose();
}
