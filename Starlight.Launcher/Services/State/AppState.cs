namespace Starlight.Launcher.Services.State;

public class AppState
{

    public event Action? OnChange;
    public event Action? Paused;
    public event Action? Resumed;

    public void CallUpdate() => OnChange?.Invoke();
    public void NotifyPaused() => Paused?.Invoke();
    public void NotifyResumed() => Resumed?.Invoke();
}
