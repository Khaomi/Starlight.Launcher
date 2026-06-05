using Foundation;
using ObjCRuntime;
using Starlight.Launcher.Models;
using System.Runtime.InteropServices;

namespace Starlight.Launcher.Services;

public sealed class MacTray : NSObject, INativeTray
{
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr Send(IntPtr r, IntPtr sel);
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr Send(IntPtr r, IntPtr sel, IntPtr a);

    private readonly List<Action?> _actions = new();
    private IntPtr _statusItem;

    public event EventHandler? IconActivated;
    public bool IsWindowVisible { get; private set; } = true;

    public void Initialize(TrayOptions o, IReadOnlyList<TrayMenuItem> menu)
    {
        var bar = Send(Class.GetHandle("NSStatusBar"), Selector.GetHandle("systemStatusBar"));
        _statusItem = Send(bar, Selector.GetHandle("statusItemWithLength:")); // -1 = variable
        var button = Send(_statusItem, Selector.GetHandle("button"));
        Send(button, Selector.GetHandle("setTitle:"), new NSString("●").Handle);

        var nsMenu = Send(Send(Class.GetHandle("NSMenu"), Selector.GetHandle("alloc")),
                          Selector.GetHandle("init"));
        for (int i = 0; i < menu.Count; i++)
        {
            _actions.Add(menu[i].Invoke);
            // create NSMenuItem, target = this, action = "onMenu:", tag = i …
        }
        Send(_statusItem, Selector.GetHandle("setMenu:"), nsMenu);
    }

    [Export("onMenu:")]
    public void OnMenu(NSObject sender) { /* _actions[tag] */ }

    public void ShowWindow() { /* NSApp setActivationPolicy:Regular + window orderFront */ IsWindowVisible = true; }
    public void HideWindow() { /* window orderOut + setActivationPolicy:Accessory */ IsWindowVisible = false; }
    public void UpdateTooltip(string t) { }
    protected override void Dispose(bool d) { /* removeStatusItem */ base.Dispose(d); }
}