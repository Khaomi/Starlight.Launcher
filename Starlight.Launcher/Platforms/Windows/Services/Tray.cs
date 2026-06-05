using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Media.Imaging;
using Starlight.Launcher.Models;
using Starlight.Launcher.Services;

namespace Starlight.Launcher.Services;

public sealed partial class WindowsTray : INativeTray
{
    private TaskbarIcon? _icon;
    private MauiWinUIWindow Win =>
        (MauiWinUIWindow)Application.Current!.Windows[0].Handler!.PlatformView!;

    public event EventHandler? IconActivated;
    public bool IsWindowVisible => Win.AppWindow.IsVisible;

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
        };
        _icon.LeftClickCommand = new RelayCommand(() => IconActivated?.Invoke(this, EventArgs.Empty));
        _icon.ForceCreate();
    }

    public void ShowWindow() { Win.AppWindow.Show(); Win.Activate(); }
    public void HideWindow() => Win.AppWindow.Hide();
    public void UpdateTooltip(string t) { if (_icon is not null) _icon.ToolTipText = t; }
    public void Dispose() => _icon?.Dispose();
}