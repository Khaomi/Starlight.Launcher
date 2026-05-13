using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Starlight.Launcher.Models.Settings;
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
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IBrowserViewportService BrowserViewportService { get; set; } = null!;

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
    private AppNavigationPosition _navigation;

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
    }


    private async Task ApplyThemeAsync()
    {
        var settings = await Settings.GetSettingsAsync();
        var prefersDark = await JS.InvokeAsync<bool>("appTheme.prefersDark");
        var themeName = ToDataTheme(settings.Theme, prefersDark);
        await JS.InvokeVoidAsync("appTheme.set", themeName);
    }

    private bool IsActive(string href, NavLinkMatch match = NavLinkMatch.Prefix)
    {
        var current = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?')[0];
        current = "/" + current.TrimEnd('/');
        var target = href.TrimEnd('/');
        if (string.IsNullOrEmpty(target)) target = "/";

        return match == NavLinkMatch.All
            ? string.Equals(current, target, StringComparison.OrdinalIgnoreCase)
            : current.StartsWith(target, StringComparison.OrdinalIgnoreCase);
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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
        => Navigation.LocationChanged += OnLocationChanged;

    public async ValueTask DisposeAsync()
    {
        await BrowserViewportService.UnsubscribeAsync(this);
        Navigation.LocationChanged -= OnLocationChanged;
        State.OnChange -= AppCalledRepaint;
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
