using Microsoft.AspNetCore.Components;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;
using System.Globalization;

namespace Starlight.Launcher.Components.Pages;

public partial class Servers : ComponentBase, IDisposable
{
    [Inject] private SettingsService _settings { get; set; } = default!;
    [Inject] private HubServerFetcher _fetcher { get; set; } = default!;
    [Inject] private LocalizationManager _localization { get; set; } = default!;
    private ServerListFilters _filters = new();
    private readonly CancellationTokenSource _disposeCts = new();

    private IReadOnlyList<ServerStatusData> _allServers = [];
    private List<ServerStatusData> _filteredServers = [];
    private IReadOnlyList<string> _availableRPTags = [];
    private IReadOnlyList<string> _availableLangTags = [];
    private IReadOnlyList<string> _availableRegionTags = [];
    private int _totalCount;

    private CancellationTokenSource? _searchDebounceCts;

    private IReadOnlySet<string> _favoriteAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private bool _bottomSearch { get; set; }
    private ElementPosition _searchBarPosition { get; set; }

    private ElementPosition _tagsBarPosition { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        _bottomSearch = settings.ServerListToolbarBottomSearch;
        _searchBarPosition = settings.ServerListToolBarSearchPosition;
        _tagsBarPosition = settings.ServerListToolBarBottomTagsPosition;
        _fetcher.ServersChanged += OnServersChanged;
        _fetcher.StatusChanged += OnStatusChanged;
        _filters = settings.CachedFilters;
        _filters.TagsExpanded = settings.ServerListToolBarTagsBarOpen;
        _filters.Changed += OnFiltersChanged;

        _favoriteAddresses = _settings.GetFavoriteAddressesSnapshot();
        _settings.FavoritesChanged += OnFavoritesChanged;

        RebuildFromFetcher();
    }

    private async void OnFavoritesChanged()
    {
        try
        {
            await InvokeAsync(() =>
            {
                _favoriteAddresses = _settings.GetFavoriteAddressesSnapshot();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
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
        _allServers = _fetcher.AllServers;
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

        if (!string.IsNullOrWhiteSpace(_filters.SearchQuery))
        {
            var q = _filters.SearchQuery.Trim();
            query = query.Where(s =>
                (s.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                s.Address.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (_filters.SelectedRP.Count > 0)
        {
            query = query.Where(s =>
            {
                var rpTag = ParseRPTag(GetTags(s).FirstOrDefault(t => t.StartsWith("rp")) ?? "");
                return _filters.SelectedRP.Contains(rpTag);
            });
        }

        if (_filters.SelectedRegion.Count > 0)
        {
            query = query.Where(s => GetRegion(s) != null && _filters.SelectedRegion.Contains(ParseRegionTag(GetRegion(s)!)));
        }

        if (_filters.SelectedLang.Count > 0)
        {
            query = query.Where(s => GetLanguage(s) != null && _filters.SelectedLang.Contains(ParseLangTag(GetLanguage(s)!)));
        }

        if (_filters.HideAdult)
            query = query.Where(s => GetTags(s) is { } tags && !(tags.Contains("18+") || tags.Contains("+18")));
        else if (_filters.OnlyAdult)
            query = query.Where(s => GetTags(s) is { } tags && (tags.Contains("18+") || tags.Contains("+18")));

        if (_filters.HideEmpty)
            query = query.Where(s => GetPlayers(s) > 0);
        if (_filters.HideFull)
            query = query.Where(s => GetPlayers(s) < GetMaxPlayers(s));

        query = _filters.SortBy switch
        {
            ServerSortMode.Players => query.OrderByDescending(GetPlayers),
            ServerSortMode.Name => query.OrderBy(s => s.Name ?? s.Address, StringComparer.OrdinalIgnoreCase),
            ServerSortMode.Ping => query.OrderBy(GetPing),
            _ => query,
        };

        _filteredServers = [.. query];

        _settings.CacheFilters(_filters).GetAwaiter().GetResult();
    }

    private void HandleRefresh() => _fetcher.RequestRefresh();

    private void ClearFilters()
    {
        _filters.SearchQuery = "";
        _filters.SelectedRP.Clear();
        _filters.SelectedLang.Clear();
        _filters.SelectedRegion.Clear();
        _filters.HideEmpty = false;
        _filters.HideFull = false;
        ApplyFilters();
    }

    private async Task HandleFavorite(ServerStatusData server)
    {
        var favorites = _settings.GetFavorites();
        var alreadyExist = favorites.FirstOrDefault(x => x.Address == server.Address);

        if ((alreadyExist == null || alreadyExist == default) && server.HubAddress != null)
        {
            favorites.Add(new FavoriteServer(server.Name, server.Address, server.HubAddress));
            await _settings.WriteFavoritesAsync(favorites);
        }
        else if (alreadyExist != null)
        {
            favorites.Remove(alreadyExist);
            await _settings.WriteFavoritesAsync(favorites);
        }
    }

    private void HandleInfoNeeded(ServerStatusData server) =>
        // Lazy-load
        ((IServerSource)_fetcher).UpdateInfoFor(server);

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
        foreach (var kvp in _rPTagTypes)
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
            var cultureId = tag[5..];
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
            var regionId = tag[7..];
            if (_regionTransformations.TryGetValue(regionId, out var regionName))
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

    private static readonly Dictionary<string, List<string>> _rPTagTypes = new()
    {
        ["NRP"] = ["rp:none", "rp:nrp", "rp"],
        ["LRP"] = ["rp:low", "rp:lrp"],
        ["MRP"] = ["rp:medium", "rp:mrp", "rp:med"],
        ["HRP"] = ["rp:high", "rp:hrp"]
    };

    private static readonly Dictionary<string, string> _regionTransformations = new()
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
        GC.SuppressFinalize(this);
        _fetcher.ServersChanged -= OnServersChanged;
        _fetcher.StatusChanged -= OnStatusChanged;
        _filters.Changed -= OnFiltersChanged;
        _settings.FavoritesChanged -= OnFavoritesChanged;
        _disposeCts.Cancel();
        _disposeCts.Dispose();
        _searchDebounceCts?.Dispose();
    }
}
