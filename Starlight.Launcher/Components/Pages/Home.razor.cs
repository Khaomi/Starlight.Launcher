using Microsoft.AspNetCore.Components;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] SettingsService Settings { get; set; } = null!;
    [Inject] HubServerFetcher Fetcher { get; set; } = null!;
    [Inject] ServerStatusCache StatusCache { get; set; } = null!;

    private List<ServerStatusData> FavoriteServers { get; set; } = null!;

    public void Dispose()
    {
        Settings.FavoritesChanged -= HandleFavorites;
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        UpdateFavorites(await Settings.GetFavoritesAsync());
        Settings.FavoritesChanged += HandleFavorites;
        await base.OnInitializedAsync();
    }

    private void UpdateFavorites(List<FavoriteServer> servers)
    {
        FavoriteServers = servers.Select(x =>
        {
            var data = StatusCache.GetStatusFor(x.Address, x.HubAddress);
            StatusCache.InitialUpdateStatus(data);
            return data;
        }).ToList();
    }

    private async void HandleFavorites()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                UpdateFavorites(await Settings.GetFavoritesAsync());
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleFavorite(ServerStatusData server)
    {
        var favorites = Settings.GetFavorites();
        var alreadyExist = favorites.FirstOrDefault(x => x.Address == server.Address);

        if ((alreadyExist == null || alreadyExist == default) && server.HubAddress != null)
        {
            favorites.Add(new FavoriteServer(server.Name, server.Address, server.HubAddress));
            await Settings.WriteFavoritesAsync(favorites);
        }
        else if (alreadyExist != null)
        {
            favorites.Remove(alreadyExist);
            await Settings.WriteFavoritesAsync(favorites);
        }
    }

    private void HandleInfoNeeded(ServerStatusData server)
    {
        // Lazy-load
        ((IServerSource)Fetcher).UpdateInfoFor(server);
    }
}