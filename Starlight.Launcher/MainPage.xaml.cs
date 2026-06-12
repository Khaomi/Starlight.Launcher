using Starlight.Launcher.Services.State;

namespace Starlight.Launcher;

public partial class MainPage : ContentPage
{
    public MainPage(AppState appState)
    {
        InitializeComponent();

        appState.Paused += () => MainThread.BeginInvokeOnMainThread(blazorWebView.PauseWebView);
        appState.Resumed += () => MainThread.BeginInvokeOnMainThread(blazorWebView.ResumeWebView);
    }
}
