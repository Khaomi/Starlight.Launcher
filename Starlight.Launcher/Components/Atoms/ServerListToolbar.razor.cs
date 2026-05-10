using Microsoft.AspNetCore.Components;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Services.ServerStatus;

namespace Starlight.Launcher.Components.Atoms;

public partial class ServerListToolbar : ComponentBase
{
    [Parameter, EditorRequired] public ServerListFilters Filters { get; set; } = null!;
    [Parameter] public RefreshListStatus Status { get; set; }
    [Parameter] public int TotalCount { get; set; }
    [Parameter] public int FilteredCount { get; set; }
    [Parameter] public IReadOnlyList<string> AvailableRPTags { get; set; } = Array.Empty<string>();
    [Parameter] public IReadOnlyList<string> AvailableLangTags { get; set; } = Array.Empty<string>();
    [Parameter] public IReadOnlyList<string> AvailableRegionTags { get; set; } = Array.Empty<string>();
    [Parameter] public EventCallback OnRefresh { get; set; }

    private bool IsRefreshing => Status == RefreshListStatus.UpdatingMaster;

    private void OnSearchChanged(string value)
    {
        Filters.SearchQuery = value ?? "";
        Filters.NotifyChanged();
    }

    private void OnSortChanged(ServerSortMode mode)
    {
        Filters.SortBy = mode;
        Filters.NotifyChanged();
    }

    private void ToggleHideEmpty()
    {
        Filters.HideEmpty = !Filters.HideEmpty;
        Filters.NotifyChanged();
    }

    private void ToggleHideFull()
    {
        Filters.HideFull = !Filters.HideFull;
        Filters.NotifyChanged();
    }

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

    private (string Text, MudBlazor.Color Color) GetStatusDisplay() => Status switch
    {
        RefreshListStatus.NotUpdated => ("Idle", MudBlazor.Color.Default),
        RefreshListStatus.UpdatingMaster => ("Updating…", MudBlazor.Color.Info),
        RefreshListStatus.Updated => ("Up to date", MudBlazor.Color.Success),
        RefreshListStatus.PartialError => ("Some hubs failed", MudBlazor.Color.Warning),
        RefreshListStatus.Error => ("Error", MudBlazor.Color.Error),
        _ => ("", MudBlazor.Color.Default),
    };
}