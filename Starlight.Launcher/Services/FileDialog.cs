namespace Starlight.Launcher.Services;

public interface IFileDialogService
{
    Task<FileResult?> PickReplayAsync(CancellationToken cancel = default);
}