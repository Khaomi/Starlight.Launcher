namespace Starlight.Launcher.Services.State;

public class AppState
{

    public event Action? OnChange;

    public void CallUpdate()
        => OnChange?.Invoke();
}