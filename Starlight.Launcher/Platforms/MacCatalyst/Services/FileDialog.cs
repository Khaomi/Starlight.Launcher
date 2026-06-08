using Foundation;
using UIKit;
using UniformTypeIdentifiers;

namespace Starlight.Launcher.Services;

public sealed class MacFileDialogService : IFileDialogService
{
    public Task<FileResult?> PickFileAsync(string filter, CancellationToken cancel = default)
        => PickAsync(BuildFileTypes(), asCopy: true, cancel);

    public Task<FileResult?> PickFolderAsync(CancellationToken cancel = default)
        => PickAsync(new[] { UTTypes.Folder }, asCopy: false, cancel);

    private static Task<FileResult?> PickAsync(UTType[] types, bool asCopy, CancellationToken cancel)
    {
        var tcs = new TaskCompletionSource<FileResult?>();

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try { ShowPicker(tcs, types, asCopy, cancel); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        });

        return tcs.Task;
    }

    private static UTType[] BuildFileTypes()
    {
        var types = new List<UTType> { UTTypes.Zip };
        if (UTType.CreateFromExtension("rt") is { } rt)
            types.Add(rt);
        return types.ToArray();
    }

    private static void ShowPicker(
        TaskCompletionSource<FileResult?> tcs,
        UTType[] types,
        bool asCopy,
        CancellationToken cancel)
    {
        var presenter = GetTopViewController();
        if (presenter is null)
        {
            tcs.TrySetResult(null);
            return;
        }

        var picker = new UIDocumentPickerViewController(types, asCopy: asCopy)
        {
            AllowsMultipleSelection = false,
            ShouldShowFileExtensions = true,
        };

        var del = new PickerDelegate(tcs, scoped: !asCopy);
        picker.Delegate = del;

        var reg = cancel.Register(() => UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            picker.DismissViewController(true, null);
            tcs.TrySetResult(null);
        }));

        tcs.Task.ContinueWith(_ =>
        {
            GC.KeepAlive(del);
            reg.Dispose();
        }, TaskScheduler.Default);

        presenter.PresentViewController(picker, true, null);
    }

    private static UIViewController? GetTopViewController()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .ToArray()
            .OfType<UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        var vc = window?.RootViewController;
        while (vc?.PresentedViewController is { } presented)
            vc = presented;

        return vc;
    }

    private sealed class PickerDelegate : UIDocumentPickerDelegate
    {
        private readonly TaskCompletionSource<FileResult?> _tcs;
        private readonly bool _scoped;

        public PickerDelegate(TaskCompletionSource<FileResult?> tcs, bool scoped)
        {
            _tcs = tcs;
            _scoped = scoped;
        }

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
        {
            if (urls.FirstOrDefault() is not { } url || url.Path is not { Length: > 0 } path)
            {
                _tcs.TrySetResult(null);
                return;
            }

            if (_scoped)
            {
                url.StartAccessingSecurityScopedResource();
                url.StopAccessingSecurityScopedResource();
            }

            _tcs.TrySetResult(new FileResult(path));
        }

        public override void WasCancelled(UIDocumentPickerViewController controller)
            => _tcs.TrySetResult(null);
    }
}
