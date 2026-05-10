using Microsoft.AspNetCore.Components;
using Starlight.Launcher.Models.ServerStatus;

namespace Starlight.Launcher.Components.Atoms;

public partial class ServerListTagsBar : ComponentBase
{
    [Parameter, EditorRequired] public ServerListFilters Filters { get; set; } = null!;
    [Parameter] public IReadOnlyList<string> AvailableRPTags { get; set; } = Array.Empty<string>();
    [Parameter] public IReadOnlyList<string> AvailableLangTags { get; set; } = Array.Empty<string>();
    [Parameter] public IReadOnlyList<string> AvailableRegionTags { get; set; } = Array.Empty<string>();

    private bool HasActiveTagFilters =>
        Filters.SelectedRP.Count > 0 ||
        Filters.SelectedLang.Count > 0 ||
        Filters.SelectedRegion.Count > 0;

    private void ToggleRP(string rpTag)
    {
        if (!Filters.SelectedRP.Add(rpTag))
            Filters.SelectedRP.Remove(rpTag);
        Filters.NotifyChanged();
    }

    private void ToggleLang(string langTag)
    {
        if (!Filters.SelectedLang.Add(langTag))
            Filters.SelectedLang.Remove(langTag);
        Filters.NotifyChanged();
    }

    private void ToggleRegion(string regionTag)
    {
        if (!Filters.SelectedRegion.Add(regionTag))
            Filters.SelectedRegion.Remove(regionTag);
        Filters.NotifyChanged();
    }

    private void ClearTagFilters()
    {
        Filters.SelectedRP.Clear();
        Filters.SelectedLang.Clear();
        Filters.SelectedRegion.Clear();
        Filters.NotifyChanged();
    }
}