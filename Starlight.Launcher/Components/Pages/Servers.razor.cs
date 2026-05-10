using Microsoft.AspNetCore.Components;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Models.ServerStatus;
using Starlight.Launcher.Services.ServerStatus;

namespace Starlight.Launcher.Components.Pages;

public partial class Servers : ComponentBase, IDisposable
{
    private readonly ServerListFilters _filters = new();
    private readonly CancellationTokenSource _disposeCts = new();

    private IReadOnlyList<ServerStatusData> _allServers = Array.Empty<ServerStatusData>();
    private List<ServerStatusData> _filteredServers = new();
    private IReadOnlyList<string> _availableTags = Array.Empty<string>();
    private IReadOnlyList<string> _availableRPTags = Array.Empty<string>();
    private IReadOnlyList<string> _availableLanguages = Array.Empty<string>();
    private int _totalCount;

    private static IReadOnlyList<string> TagsWhitelist = [];

    private static IReadOnlyList<string> TagsBlacklist = [];

    private CancellationTokenSource? _searchDebounceCts;

    protected override void OnInitialized()
    {
        Fetcher.ServersChanged += OnServersChanged;
        Fetcher.StatusChanged += OnStatusChanged;
        _filters.Changed += OnFiltersChanged;

        _allServers = Fetcher.AllServers;
        ApplyFilters();

        Fetcher.RequestInitialUpdate();
    }

    private async void OnServersChanged()
    {
        try
        {
            await InvokeAsync(() =>
            {
                _allServers = Fetcher.AllServers;
                _totalCount = _allServers.Count;
                ExtractTags(_allServers, out _availableTags, out _availableRPTags);
                ApplyFilters();
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { /* page disposed */ }
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

        if (_filters.SelectedTags.Count > 0)
        {
            query = query.Where(s => GetTags(s).Any(t => _filters.SelectedTags.Contains(t)));
        }

        if (_filters.SelectedRP.Count > 0)
        {
            query = query.Where(s =>
            {
                var rpTag = ParseRPTag(GetTags(s).FirstOrDefault(t => t.StartsWith("rp")) ?? "");
                return _filters.SelectedRP.Contains(rpTag);
            });
        }

        if (!string.IsNullOrEmpty(_filters.Region))
        {
            query = query.Where(s => GetRegion(s) == _filters.Region);
        }

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

        _filteredServers = query.ToList();
    }

    private void HandleRefresh() => Fetcher.RequestRefresh();

    private void ClearFilters()
    {
        _filters.SearchQuery = "";
        _filters.SelectedTags.Clear();
        _filters.Region = null;
        _filters.HideEmpty = false;
        _filters.HideFull = false;
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

    private static void ExtractTags(IEnumerable<ServerStatusData> servers, out IReadOnlyList<string> tags, out IReadOnlyList<string> rpTags)
    {
        var allTags = servers
            .SelectMany(GetTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
        tags = allTags.Where(t => !t.StartsWith("rp")).ToList();
        rpTags = [.. allTags.Where(t => t.StartsWith("rp")).Select(x => ParseRPTag(x)).Distinct()];
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

    private static Dictionary<string, List<string>> RPTagTypes = new()
    {
        ["NRP"] = new List<string> { "rp:none", "rp:nrp", "rp" },
        ["LRP"] = new List<string> { "rp:low", "rp:lrp" },
        ["MRP"] = new List<string> { "rp:medium", "rp:mrp", "rp:med" },
        ["HRP"] = new List<string> { "rp:high", "rp:hrp" }

    };

    private static int GetPlayers(ServerStatusData s) => s.PlayerCount;
    private static int GetMaxPlayers(ServerStatusData s) => s.SoftMaxPlayerCount;
    private static int GetPing(ServerStatusData s) => (int?)(s.Ping?.TotalMilliseconds) ?? 0;
    private static IEnumerable<string> GetTags(ServerStatusData s) => s.Tags.Select(t => t.StartsWith("region:") || t.StartsWith("lang:") ? null : t).Where(t => t != null)!;
    private static string? GetRegion(ServerStatusData s) => s.Tags.FirstOrDefault(x => x.StartsWith("region:"));

    public void Dispose()
    {
        Fetcher.ServersChanged -= OnServersChanged;
        Fetcher.StatusChanged -= OnStatusChanged;
        _filters.Changed -= OnFiltersChanged;
        _disposeCts.Cancel();
        _disposeCts.Dispose();
        _searchDebounceCts?.Dispose();
    }
}