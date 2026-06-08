using System.IO;

namespace Robust.Launcher.Api.Utility;

public static class TempFile
{
    public static FileStream CreateTempFile() => new TempFileStream(Path.GetTempFileName());

    private sealed class TempFileStream : FileStream
    {
        public TempFileStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            File.Delete(Name);
        }
    }
}
