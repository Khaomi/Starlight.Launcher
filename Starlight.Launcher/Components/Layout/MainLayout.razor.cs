using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Discord;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;

using AppTheme = Starlight.Launcher.Models.Settings.AppTheme;

namespace Starlight.Launcher.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable, IBrowserViewportObserver
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private SettingsService Settings { get; set; } = null!;
    [Inject] private LocalizationManager Localization { get; set; } = null!;
    [Inject] private AppState State { get; set; } = null!;
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = null!;
    [Inject] private INativeTray Tray { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private DiscordRichPresence Presence { get; set; } = null!;

    Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

    private bool _isSmallScreen = false;

    private static string ToDataTheme(AppTheme t, bool systemPrefersDark) => t switch
    {
        AppTheme.EmeraldLight => "emerald-light",
        AppTheme.EmeraldDark => "emerald-dark",
        AppTheme.AmberLight => "amber-light",
        AppTheme.Midnight => "midnight",
        AppTheme.System => systemPrefersDark ? "emerald-dark" : "emerald-light",
        _ => "emerald-light"
    };

    private ErrorBoundary? _errorBoundary;
    private ElementPosition _navigation;

    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    protected override async Task OnInitializedAsync()
    {
        var settings = await Settings.GetSettingsAsync();
        await ApplyThemeAsync();
        _navigation = settings.Navigation;
        State.OnChange += AppCalledRepaint;
        Navigation.LocationChanged += OnLocationChanged;

        if (settings.CollapseInTrayOnStart)
            Tray.HideWindow(); // If layout is initialized - window exists, so we can hide it right away if the user wants that.
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var uri = new Uri(e.Location);
        switch (uri.AbsolutePath)
        {
            case "/servers":
                Presence.UpdatePresence(PresenceState.SearchingServers);
                break;
            case "/settings":
                Presence.UpdatePresence(PresenceState.SettingUp);
                break;
            default:
                Presence.UpdatePresence(PresenceState.Idle);
                break;
        }
    }


    private async Task ApplyThemeAsync()
    {
        var settings = await Settings.GetSettingsAsync();
        var prefersDark = await JS.InvokeAsync<bool>("appTheme.prefersDark");
        var themeName = ToDataTheme(settings.Theme, prefersDark);
        await JS.InvokeVoidAsync("appTheme.set", themeName);
    }

    private void AppCalledRepaint()
    {
        _ = InvokeAsync(async () =>
        {
            var settings = await Settings.GetSettingsAsync();
            await ApplyThemeAsync();
            _navigation = settings.Navigation;
            StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);
        State.OnChange -= AppCalledRepaint;
        Navigation.LocationChanged -= OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
    {
        NotifyOnBreakpointOnly = true
    };

    Task IBrowserViewportObserver.NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
    {
        _isSmallScreen = browserViewportEventArgs.Breakpoint <= Breakpoint.Sm;
        return InvokeAsync(StateHasChanged);
    }
}
