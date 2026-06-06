using System.Runtime.InteropServices;
using Robust.Launcher.Api.Models.Data;

namespace Starlight.Launcher.Services;

public sealed class WindowsFileDialogService : IFileDialogService
{
    public Task<FileResult?> PickReplayAsync(CancellationToken cancel = default)
    {
        var tcs = new TaskCompletionSource<FileResult?>();

        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(ShowDialog());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    private FileResult? ShowDialog()
    {
        var buffer = Marshal.AllocHGlobal(1024 * sizeof(char));

        try
        {
            Marshal.WriteInt16(buffer, 0);

            var ofn = new OpenFileName
            {
                lStructSize = Marshal.SizeOf<OpenFileName>(),
                lpstrFilter = "Content bundles / replays\0*.zip;*.rt\0Все файлы\0*.*\0\0",
                lpstrFile = buffer,
                nMaxFile = 1024,
                lpstrTitle = "Select roleplay or content-bundle",
                Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
            };

            if (!GetOpenFileNameW(ref ofn))
                return null; // Null if cancelled or error.

            var path = Marshal.PtrToStringUni(buffer) ?? string.Empty;
            return new FileResult(path);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }


    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_NOCHANGEDIR = 0x00000008;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string? lpstrFilter;
        public string? lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public string? lpstrFileTitle;
        public int nMaxFileTitle;
        public string? lpstrInitialDir;
        public string? lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string? lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string? lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OpenFileName ofn);
}