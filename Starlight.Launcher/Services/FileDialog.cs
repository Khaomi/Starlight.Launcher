namespace Starlight.Launcher.Services;

public interface IFileDialogService
{
    Task<FileResult?> PickFileAsync(string filter, CancellationToken cancel = default);
    Task<FileResult?> PickFolderAsync(CancellationToken cancel = default);
}
