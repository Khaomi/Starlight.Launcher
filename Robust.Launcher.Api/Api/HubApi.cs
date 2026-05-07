using Robust.Launcher.Api.Models;
using Robust.Launcher.Api.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Launcher.Api.Api;

public sealed class HubApi
{
    private readonly HttpClient _http;
    private readonly ILogger<HubApi> _logger;

    public HubApi(HttpClient http, ILogger<HubApi> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ServerListEntry[]> GetServers(UrlFallbackSet hubUri, CancellationToken cancel)
    {
        // Sanity check, this should be enforced with code
        if (!hubUri.Urls.All(u => u.EndsWith('/')))
            throw new Exception("URI doesn't have trailing slash");

        var finalUrl = hubUri + "api/servers";

        return await finalUrl.GetFromJsonAsync<ServerListEntry[]>(_http, cancel)
               ?? throw new JsonException("Server list is null!");
    }

    public async Task<ServerInfo> GetServerInfo(
        string serverAddress,
        string hubAddress,
        CancellationToken cancel)
    {
        var url =
            $"{hubAddress}api/servers/info?url={Uri.EscapeDataString(serverAddress)}";

        try
        {
#if DEBUG
            _logger.LogDebug(
                "Requesting server info. ServerAddress: {ServerAddress}, Url: {Url}",
                serverAddress,
                url);
#endif

            var result = await _http.GetFromJsonAsync<ServerInfo>(url, cancel);

            if (result is null)
            {
                _logger.LogWarning(
                    "Server info response was null. ServerAddress: {ServerAddress}",
                    serverAddress);

                throw new InvalidDataException(
                    $"Server info response was null for '{serverAddress}'.");
            }

#if DEBUG
            _logger.LogDebug(
                "Successfully received server info. ServerAddress: {ServerAddress}",
                serverAddress);
#endif

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Server info request cancelled. ServerAddress: {ServerAddress}",
                serverAddress);

            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while requesting server info. ServerAddress: {ServerAddress}, Url: {Url}",
                serverAddress,
                url);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while requesting server info. ServerAddress: {ServerAddress}",
                serverAddress);

            throw;
        }
    }

    public sealed record ServerListEntry(string Address, ServerApi.ServerStatus StatusData);
}