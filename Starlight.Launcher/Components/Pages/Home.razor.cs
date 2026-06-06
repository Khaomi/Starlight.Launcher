using Microsoft.AspNetCore.Components;
using MudBlazor;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Components.Atoms;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Starlight.Launcher.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] SettingsService Settings { get; set; } = null!;
    [Inject] HubServerFetcher Fetcher { get; set; } = null!;
    [Inject] ServerStatusCache StatusCache { get; set; } = null!;
    [Inject] Connector Connector { get; set; } = null!;
    [Inject] IDialogService DialogService { get; set; } = null!;
    [Inject] IFileDialogService FileDialog { get; set; } = null!;
    [Inject] NavigationManager Nav { get; set; } = null!;

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
        => ((IServerSource)Fetcher).UpdateInfoFor(server);

    private async Task OpenDirectConnect()
    {
        var dialog = await DialogService.ShowAsync<DirectConnectDialog>(
            "Direct Connect");
        var dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
            return;

        var result = (DirectConnectResult)dialogResult.Data!;

        if (result.AddToFavorites)
            await AddDirectFavorite(result.Address);

        await ShowConnecting(p => p.Add(x => x.Address, result.Address));
    }

    private async Task LoadReplay()
    {
        var file = await FileDialog.PickReplayAsync();
        if (file is null)
            return;

        Connector.LaunchContentBundle(file);
    }

    private async Task AddDirectFavorite(string address)
    {
        var favorites = Settings.GetFavorites();
        if (favorites.Any(x => x.Address == address))
            return;

        favorites.Add(new FavoriteServer(address, address, ""));
        await Settings.WriteFavoritesAsync(favorites);
    }
    private Task ShowConnecting(Action<DialogParameters<ConnectingDialog>> configure)
    {
        var parameters = new DialogParameters<ConnectingDialog>();
        configure(parameters);

        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            BackdropClick = false,
            CloseButton = false,
        };

        return DialogService.ShowAsync<ConnectingDialog>("Connecting", parameters, options);
    }
}