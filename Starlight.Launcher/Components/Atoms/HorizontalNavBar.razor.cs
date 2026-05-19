using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Starlight.Launcher.Services.Localization;

namespace Starlight.Launcher.Components.Atoms;

public sealed partial class HorizontalNavBar : ComponentBase, IDisposable
{
    [Inject] private LocalizationManager Localization { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

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

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => InvokeAsync(StateHasChanged);

    protected override void OnInitialized()
        => Navigation.LocationChanged += OnLocationChanged;

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }
}