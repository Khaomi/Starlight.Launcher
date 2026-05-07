using Robust.Launcher.Api.Api;
using Robust.Launcher.Api.Models.ServerStatus;
using Robust.Launcher.Api.Utility;
using Serilog;
using Starlight.Launcher.Services.Settings;
using static Robust.Launcher.Api.Api.HubApi;

namespace Starlight.Launcher.Services.ServerStatus;

public sealed class HubServerFetcher : IServerSource, IDisposable
{
    private readonly HubApi _hubApi;
    private readonly SettingsService _settings;
    private readonly ServerStatusCache _cache;

    private CancellationTokenSource? _refreshCancel;

    private List<ServerStatusData> _allServers = new();
    private readonly object _serversLock = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly SemaphoreSlim _infoThrottle = new(initialCount: 4, maxCount: 4);

    private readonly Dictionary<string, DateTimeOffset> _hubBackoffUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _backoffLock = new();

    public IReadOnlyList<ServerStatusData> AllServers
    {
        get { lock (_serversLock) return _allServers.ToList(); }
    }

    public event Action? ServersChanged;
    public event Action<RefreshListStatus>? StatusChanged;

    public RefreshListStatus Status { get; private set; } = RefreshListStatus.NotUpdated;

    public HubServerFetcher(HubApi hub, SettingsService settings, ServerStatusCache cache)
    {
        _hubApi = hub;
        _settings = settings;
        _cache = cache;
    }

    /// <summary>
    /// This function requests the initial update from the server if one hasn't already been requested.
    /// </summary>
    public void RequestInitialUpdate()
    {
        _lock.Wait();
        try
        {
            if (Status == RefreshListStatus.NotUpdated)
                RequestRefresh();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// This function performs a refresh.
    /// </summary>
    public void RequestRefresh()
    {
        _refreshCancel?.Cancel();
        _refreshCancel = new CancellationTokenSource(10000);
        RefreshServerList(_refreshCancel.Token);
    }

    private bool IsHubBackedOff(string hubAddress, out TimeSpan remaining)
    {
        lock (_backoffLock)
        {
            if (_hubBackoffUntil.TryGetValue(hubAddress, out var until))
            {
                remaining = until - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                    return true;
                _hubBackoffUntil.Remove(hubAddress);
            }
        }
        remaining = TimeSpan.Zero;
        return false;
    }

    private void SetHubBackoff(string hubAddress, TimeSpan duration)
    {
        lock (_backoffLock)
            _hubBackoffUntil[hubAddress] = DateTimeOffset.UtcNow + duration;
        Log.Warning("Hub {Hub} rate-limited, backing off for {Duration}", hubAddress, duration);
    }

    private async void RefreshServerList(CancellationToken cancel)
    {
        lock (_serversLock)
            _allServers.Clear();
        Status = RefreshListStatus.UpdatingMaster;

        try
        {
            var entries = new Dictionary<string, HubServerListEntry>(StringComparer.OrdinalIgnoreCase);
            var requests = new List<(Task<ServerListEntry[]> Request, Uri Hub)>();
            var allSucceeded = true;

            // Queue requests
            var settings = await _settings.GetSettingsAsync();
            foreach (var hub in settings.Hubs.OrderBy(h => h.Priority))
            {
                requests.Add((_hubApi.GetServers(UrlFallbackSet.FromSingle(hub.HubUri), cancel), hub.HubUri));
            }

            // Await all requests
            await Task.WhenAll(requests.Select(r => r.Request.ContinueWith(_ => { },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default)))
                .ConfigureAwait(false);

            cancel.ThrowIfCancellationRequested();

            // Process responses
            foreach (var (request, hub) in requests)
            {
                if (!request.IsCompletedSuccessfully)
                {
                    if (request.IsFaulted)
                    {
                        // request.Exception is non-null, see https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.isfaulted?view=net-7.0#remarks
                        foreach (var ex in request.Exception!.InnerExceptions)
                        {
                            Log.Warning("Request to hub {HubAddress} failed: {Message}", hub, ex.Message);
                        }
                    }
                    else if (request.IsCanceled)
                    {
                        Log.Warning("Request to hub {HubAddress} failed: canceled", hub);
                    }

                    allSucceeded = false;
                    continue;
                }

                foreach (var entry in request.Result)
                {
                    // Don't add server if it was already provided by another hub with higher priority
                    var maybeNewEntry = new HubServerListEntry(entry.Address, hub.AbsoluteUri, entry.StatusData);
                    if (!entries.TryAdd(entry.Address, maybeNewEntry))
                    {
                        Log.Verbose("Not adding {Entry} from {ThisHub} because it was already provided by {PreviousHub}",
                            entry.Address, hub.AbsoluteUri, entries[entry.Address].HubAddress);
                    }
                }
            }

            lock (_serversLock)
                _allServers.AddRange(entries.Select(entry =>
                {
                    var statusData = new ServerStatusData(entry.Value.Address, entry.Value.HubAddress);
                    ServerStatusCache.ApplyStatus(statusData, entry.Value.StatusData);
                    return statusData;
                }));

            ServersChanged?.Invoke();

            if (_allServers.Count == 0)
                // We did not get any servers
                Status = RefreshListStatus.Error;
            else if (!allSucceeded)
                // Some hubs succeeded and returned data
                Status = RefreshListStatus.PartialError;
            else
                Status = RefreshListStatus.Updated;
        }
        catch (OperationCanceledException)
        {
            Log.Debug("Server list refresh was cancelled");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to fetch server list due to exception");
            Status = RefreshListStatus.Error;
        }
    }

    void IServerSource.UpdateInfoFor(ServerStatusData statusData)
    {
        if (statusData.HubAddress == null)
        {
            Log.Error("Tried to get server info for hubbed server {Name} without HubAddress set", statusData.Name);
            return;
        }

        _ = FireAndForgetUpdateInfo(statusData);
    }

    private async Task FireAndForgetUpdateInfo(ServerStatusData statusData)
    {
        try
        {
            await _cache.UpdateInfoForCore(
                statusData,
                async token =>
                {
                    await _infoThrottle.WaitAsync(token).ConfigureAwait(false);
                    try
                    {
                        return await _hubApi
                            .GetServerInfo(statusData.Address, statusData.HubAddress!, token)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        _infoThrottle.Release();
                    }
                })
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unhandled exception fetching info for {Address}", statusData.Address);
            statusData.StatusInfo = ServerStatusInfoCode.Error;
        }
    }

    public void Dispose()
    {
        _infoThrottle.Dispose();
        _lock.Dispose();
    }
}

public enum RefreshListStatus
{
    /// <summary>
    /// Hasn't started updating yet?
    /// </summary>
    NotUpdated,

    /// <summary>
    /// Fetching master server list.
    /// </summary>
    UpdatingMaster,

    /// <summary>
    /// Fetched information from ALL servers from the hub.
    /// </summary>
    Updated,

    /// <summary>
    /// A connection error occured when fetching from at least one hub.
    /// </summary>
    PartialError,

    /// <summary>
    /// An error occured.
    /// </summary>
    Error,
}

public sealed record HubServerListEntry(string Address, string HubAddress, ServerApi.ServerStatus StatusData);