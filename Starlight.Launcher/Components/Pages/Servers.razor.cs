using Microsoft.AspNetCore.Components;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;
using System.Globalization;

namespace Starlight.Launcher.Components.Pages;

public partial class Servers : ComponentBase, IDisposable
{
    [Inject] SettingsService Settings { get; set; } = null!;
    [Inject] LocalizationManager Localization { get; set; } = null!;
    private ServerListFilters Filters = new();
    private readonly CancellationTokenSource _disposeCts = new();

    private IReadOnlyList<ServerStatusData> _allServers = Array.Empty<ServerStatusData>();
    private List<ServerStatusData> _filteredServers = new();
    private IReadOnlyList<string> _availableRPTags = Array.Empty<string>();
    private IReadOnlyList<string> _availableLangTags = Array.Empty<string>();
    private IReadOnlyList<string> _availableRegionTags = Array.Empty<string>();
    private int _totalCount;

    private CancellationTokenSource? _searchDebounceCts;

    private bool BottomSearch { get; set; }
    private bool BottomSearchBar { get; set; }

    private bool BottomTagsBar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var settings = await Settings.GetSettingsAsync();
        BottomSearch = settings.ServerListToolbarBottomSearch;
        BottomSearchBar = settings.ServerListToolBarBottomSearchBar;
        BottomTagsBar = settings.ServerListToolBarBottomTagsBar;
        Fetcher.ServersChanged += OnServersChanged;
        Fetcher.StatusChanged += OnStatusChanged;
        Filters = settings.CachedFilters;
        Filters.TagsExpanded = settings.ServerListToolBarTagsBarOpen;
        Filters.Changed += OnFiltersChanged;

        RebuildFromFetcher();

        Fetcher.RequestInitialUpdate();
    }

    private async void OnServersChanged()
    {
        try
        {
            await InvokeAsync(() =>
            {
                RebuildFromFetcher();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { /* page disposed */ }
    }

    private void RebuildFromFetcher()
    {
        _allServers = Fetcher.AllServers;
        _totalCount = _allServers.Count;
        ExtractTags(_allServers, out _availableRPTags, out _availableLangTags, out _availableRegionTags);
        ApplyFilters();
    }

    private async void OnStatusChanged(RefreshListStatus _)
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException) { }
    }

    private void OnFiltersChanged()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
        var token = _searchDebounceCts.Token;

        _ = DebounceFiltersAsync(token);
    }

    private async Task DebounceFiltersAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(150, token);
            await InvokeAsync(() =>
            {
                ApplyFilters();
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) { }
    }

    private void ApplyFilters()
    {
        IEnumerable<ServerStatusData> query = _allServers;

        if (!string.IsNullOrWhiteSpace(Filters.SearchQuery))
        {
            var q = Filters.SearchQuery.Trim();
            query = query.Where(s =>
                (s.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                s.Address.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (Filters.SelectedRP.Count > 0)
        {
            query = query.Where(s =>
            {
                var rpTag = ParseRPTag(GetTags(s).FirstOrDefault(t => t.StartsWith("rp")) ?? "");
                return Filters.SelectedRP.Contains(rpTag);
            });
        }

        if (Filters.SelectedRegion.Count > 0)
        {
            query = query.Where(s => GetRegion(s) != null && Filters.SelectedRegion.Contains(ParseRegionTag(GetRegion(s)!)));
        }

        if (Filters.SelectedLang.Count > 0)
        {
            query = query.Where(s => GetLanguage(s) != null && Filters.SelectedLang.Contains(ParseLangTag(GetLanguage(s)!)));
        }

        if (Filters.HideAdult)
            query = query.Where(s => GetTags(s) is { } tags && !(tags.Contains("18+") || tags.Contains("+18")));
        else if (Filters.OnlyAdult)
            query = query.Where(s => GetTags(s) is { } tags && (tags.Contains("18+") || tags.Contains("+18")));

        if (Filters.HideEmpty)
            query = query.Where(s => GetPlayers(s) > 0);
        if (Filters.HideFull)
            query = query.Where(s => GetPlayers(s) < GetMaxPlayers(s));

        query = Filters.SortBy switch
        {
            ServerSortMode.Players => query.OrderByDescending(GetPlayers),
            ServerSortMode.Name => query.OrderBy(s => s.Name ?? s.Address, StringComparer.OrdinalIgnoreCase),
            ServerSortMode.Ping => query.OrderBy(GetPing),
            _ => query,
        };

        _filteredServers = [.. query];

        Settings.CacheFilters(Filters).GetAwaiter().GetResult();
    }

    private void HandleRefresh() => Fetcher.RequestRefresh();

    private void ClearFilters()
    {
        Filters.SearchQuery = "";
        Filters.SelectedRP.Clear();
        Filters.SelectedLang.Clear();
        Filters.SelectedRegion.Clear();
        Filters.HideEmpty = false;
        Filters.HideFull = false;
        ApplyFilters();
    }

    private void HandleServerClick(ServerStatusData server)
    {
        // TODO: navigate to server detail or trigger join
    }

    private void HandleInfoNeeded(ServerStatusData server)
    {
        // Lazy-load
        ((IServerSource)Fetcher).UpdateInfoFor(server);
    }

    private static void ExtractTags(IEnumerable<ServerStatusData> servers, out IReadOnlyList<string> rpTags, out IReadOnlyList<string> langTags, out IReadOnlyList<string> regionTags)
    {
        var allTags = servers
            .SelectMany(GetTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
        rpTags = [.. allTags.Where(t => t.StartsWith("rp")).Select(ParseRPTag).Distinct()];
        langTags = [.. allTags.Where(t => t.StartsWith("lang:")).Select(ParseLangTag).Distinct()];
        regionTags = [.. allTags.Where(t => t.StartsWith("region:")).Select(ParseRegionTag).Distinct()];
    }

    public static string ParseRPTag(string tag)
    {
        foreach (var kvp in RPTagTypes)
        {
            if (kvp.Value.Contains(tag, StringComparer.OrdinalIgnoreCase))
                return kvp.Key;
        }

        return tag;
    }

    public static string ParseLangTag(string tag)
    {
        if (tag.StartsWith("lang:", StringComparison.OrdinalIgnoreCase))
        {
            var cultureId = tag.Substring(5);
            var culture = CultureInfo.GetCultureInfo(cultureId);
            return culture.TwoLetterISOLanguageName == culture.Name
                ? culture.EnglishName
                : CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName).EnglishName;
        }
        return tag;
    }

    public static string ParseRegionTag(string tag)
    {
        if (tag.StartsWith("region:", StringComparison.OrdinalIgnoreCase))
        {
            var regionId = tag.Substring(7);
            if (RegionTransformations.TryGetValue(regionId, out var regionName))
                return regionName;

            try
            {
                var region = new RegionInfo(regionId);
                return region.TwoLetterISORegionName == region.Name
                    ? region.EnglishName
                    : new RegionInfo(region.TwoLetterISORegionName).EnglishName;
            }
            catch
            {
                return tag;
            }
        }
        return tag;
    }

    private static Dictionary<string, List<string>> RPTagTypes = new()
    {
        ["NRP"] = new List<string> { "rp:none", "rp:nrp", "rp" },
        ["LRP"] = new List<string> { "rp:low", "rp:lrp" },
        ["MRP"] = new List<string> { "rp:medium", "rp:mrp", "rp:med" },
        ["HRP"] = new List<string> { "rp:high", "rp:hrp" }

    };

    private static Dictionary<string, string> RegionTransformations = new()
    {
        ["af_c"] = "Africa Central",
        ["af_n"] = "Africa North",
        ["af_s"] = "Africa South",
        ["ata"] = "Antarctica",
        ["as_e"] = "Asia East",
        ["as_n"] = "Asia North",
        ["as_se"] = "Asia South East",
        ["am_c"] = "America Central",
        ["eu_e"] = "Europe East",
        ["eu_w"] = "Europe West",
        ["grl"] = "Greenland",
        ["ind"] = "India",
        ["me"] = "Middle East", // Wizdens, whyyy???
        ["luna"] = "Moon", // Wizdens, whyyy???
        ["am_n_c"] = "North America Central",
        ["am_n_e"] = "North America East",
        ["am_n_w"] = "North America West",
        ["oce"] = "Oceania",
        ["am_s_e"] = "South America East",
        ["am_s_s"] = "South America South",
        ["am_s_w"] = "South America West",
        ["eu"] = "Europe"
    };

    private static int GetPlayers(ServerStatusData s) => s.PlayerCount;
    private static int GetMaxPlayers(ServerStatusData s) => s.SoftMaxPlayerCount;
    private static int GetPing(ServerStatusData s) => (int?)(s.Ping?.TotalMilliseconds) ?? 0;
    private static IEnumerable<string> GetTags(ServerStatusData s) => s.Tags.Where(t => !string.IsNullOrWhiteSpace(t))!;
    private static string? GetLanguage(ServerStatusData s) => s.Tags.FirstOrDefault(x => x.StartsWith("lang:"));
    private static string? GetRegion(ServerStatusData s) => s.Tags.FirstOrDefault(x => x.StartsWith("region:"));

    public void Dispose()
    {
        Fetcher.ServersChanged -= OnServersChanged;
        Fetcher.StatusChanged -= OnStatusChanged;
        Filters.Changed -= OnFiltersChanged;
        _disposeCts.Cancel();
        _disposeCts.Dispose();
        _searchDebounceCts?.Dispose();
    }
}