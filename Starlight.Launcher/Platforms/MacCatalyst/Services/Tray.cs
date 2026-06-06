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
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr SendCG(IntPtr r, IntPtr sel, double a);
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr Send3(IntPtr r, IntPtr sel, IntPtr a, IntPtr b, IntPtr c);
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern void SendB(IntPtr r, IntPtr sel, byte a);

    [DllImport("/usr/lib/libSystem.dylib")]
    static extern IntPtr dlopen(string path, int mode);

    const double NSVariableStatusItemLength = -1.0;
    const int RTLD_NOW = 2;

    private readonly List<Action?> _actions = new();
    private IntPtr _statusItem;

    public event EventHandler? IconActivated;
    public bool IsWindowVisible { get; private set; } = true;

    static MacTray()
    {
        dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RTLD_NOW);
    }

    public void Initialize(TrayOptions o, IReadOnlyList<TrayMenuItem> menu)
    {
        var bar = Send(Class.GetHandle("NSStatusBar"), Selector.GetHandle("systemStatusBar"));

        _statusItem = SendCG(bar, Selector.GetHandle("statusItemWithLength:"), NSVariableStatusItemLength);

        var button = Send(_statusItem, Selector.GetHandle("button"));
        Send(button, Selector.GetHandle("setTitle:"), new NSString("●").Handle);
        if (!string.IsNullOrEmpty(o.Tooltip))
            Send(button, Selector.GetHandle("setToolTip:"), new NSString(o.Tooltip).Handle);

        var nsMenu = Send(Send(Class.GetHandle("NSMenu"), Selector.GetHandle("alloc")),
                          Selector.GetHandle("init"));

        var onMenuSel = Selector.GetHandle("onMenu:");
        for (int i = 0; i < menu.Count; i++)
        {
            _actions.Add(menu[i].Invoke);

            IntPtr item;
            if (menu[i].IsSeparator)
            {
                item = Send(Class.GetHandle("NSMenuItem"), Selector.GetHandle("separatorItem"));
            }
            else
            {
                var alloc = Send(Class.GetHandle("NSMenuItem"), Selector.GetHandle("alloc"));
                item = Send3(alloc, Selector.GetHandle("initWithTitle:action:keyEquivalent:"),
                             new NSString(menu[i].Text).Handle,
                             onMenuSel,
                             new NSString(string.Empty).Handle);

                Send(item, Selector.GetHandle("setTarget:"), Handle);
                Send(item, Selector.GetHandle("setTag:"), (IntPtr)i);
            }

            Send(nsMenu, Selector.GetHandle("addItem:"), item);
            if (!menu[i].IsSeparator)
                Send(item, Selector.GetHandle("release"));
        }

        Send(_statusItem, Selector.GetHandle("setMenu:"), nsMenu);
        Send(nsMenu, Selector.GetHandle("release"));
    }

    [Export("onMenu:")]
    public void OnMenu(NSObject sender)
    {
        var tag = (int)Send(sender.Handle, Selector.GetHandle("tag")).ToInt64();
        if (tag >= 0 && tag < _actions.Count)
            _actions[tag]?.Invoke();
    }

    public void ShowWindow()
    {
        var app = Send(Class.GetHandle("NSApplication"), Selector.GetHandle("sharedApplication"));
        Send(app, Selector.GetHandle("setActivationPolicy:"), (IntPtr)0);
        SendB(app, Selector.GetHandle("activateIgnoringOtherApps:"), 1);
        IsWindowVisible = true;
    }

    public void HideWindow()
    {
        var app = Send(Class.GetHandle("NSApplication"), Selector.GetHandle("sharedApplication"));
        Send(app, Selector.GetHandle("setActivationPolicy:"), (IntPtr)1);
        IsWindowVisible = false;
    }

    public void UpdateTooltip(string t)
    {
        if (_statusItem == IntPtr.Zero) return;
        var button = Send(_statusItem, Selector.GetHandle("button"));
        Send(button, Selector.GetHandle("setToolTip:"), new NSString(t ?? string.Empty).Handle);
    }

    protected override void Dispose(bool d)
    {
        if (_statusItem != IntPtr.Zero)
        {
            var bar = Send(Class.GetHandle("NSStatusBar"), Selector.GetHandle("systemStatusBar"));
            Send(bar, Selector.GetHandle("removeStatusItem:"), _statusItem);
            _statusItem = IntPtr.Zero;
        }
        base.Dispose(d);
    }
}