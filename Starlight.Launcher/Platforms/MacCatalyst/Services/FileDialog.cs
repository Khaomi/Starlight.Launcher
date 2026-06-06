using Foundation;
using UIKit;
using UniformTypeIdentifiers;
using Robust.Launcher.Api.Models.Data;

namespace Starlight.Launcher.Services;

public sealed class MacFileDialogService : IFileDialogService
{
    public Task<FileResult?> PickReplayAsync(CancellationToken cancel = default)
    {
        var tcs = new TaskCompletionSource<FileResult?>();

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                ShowPicker(tcs, cancel);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    private static void ShowPicker(TaskCompletionSource<FileResult?> tcs, CancellationToken cancel)
    {
        var presenter = GetTopViewController();
        if (presenter is null)
        {
            tcs.TrySetResult(null);
            return;
        }

        var types = new List<UTType> { UTTypes.Zip };
        if (UTType.CreateFromExtension("rt") is { } rt)
            types.Add(rt);

        var picker = new UIDocumentPickerViewController(types.ToArray(), asCopy: true)
        {
            AllowsMultipleSelection = false,
            ShouldShowFileExtensions = true,
        };

        var del = new PickerDelegate(tcs);
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

        public PickerDelegate(TaskCompletionSource<FileResult?> tcs) => _tcs = tcs;

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
        {
            if (urls.FirstOrDefault()?.Path is not { Length: > 0 } path)
            {
                _tcs.TrySetResult(null);
                return;
            }

            _tcs.TrySetResult(new FileResult(path));
        }

        public override void WasCancelled(UIDocumentPickerViewController controller)
            => _tcs.TrySetResult(null);
    }
}