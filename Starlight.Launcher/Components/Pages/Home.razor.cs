using Microsoft.AspNetCore.Components;
using MudBlazor;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Components.Atoms;
using Starlight.Launcher.Models.Data;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] private SettingsService _settings { get; set; } = default!;
    [Inject] private HubServerFetcher _fetcher { get; set; } = default!;
    [Inject] private ServerStatusCache _statusCache { get; set; } = default!;
    [Inject] private Connector _connector { get; set; } = default!;
    [Inject] private IDialogService _dialogService { get; set; } = default!;
    [Inject] private IFileDialogService _fileDialog { get; set; } = default!;
    [Inject] private LocalizationManager _localization { get; set; } = default!;

    private List<ServerStatusData> _favoriteServers { get; set; } = null!;

    public void Dispose()
    {
        _settings.FavoritesChanged -= HandleFavorites;
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        UpdateFavorites(await _settings.GetFavoritesAsync());
        _settings.FavoritesChanged += HandleFavorites;
        await base.OnInitializedAsync();
    }

    private void UpdateFavorites(List<FavoriteServer> servers)
        => _favoriteServers = servers.Select(x =>
            {
                var data = _statusCache.GetStatusFor(x.Address, x.HubAddress);
                _statusCache.InitialUpdateStatus(data);
                return data;
            }).ToList();

    private async void HandleFavorites()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                UpdateFavorites(await _settings.GetFavoritesAsync());
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
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

    private void HandleInfoNeeded(ServerStatusData server) 
        => ((IServerSource)_fetcher).UpdateInfoFor(server);

    private async Task OpenDirectConnect()
    {
        var dialog = await _dialogService.ShowAsync<DirectConnectDialog>(
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
        var file = await _fileDialog.PickFileAsync();
        if (file is null)
            return;

        _connector.LaunchContentBundle(file);
    }

    private async Task AddDirectFavorite(string address)
    {
        var favorites = _settings.GetFavorites();
        if (favorites.Any(x => x.Address == address))
            return;

        favorites.Add(new FavoriteServer(address, address, ""));
        await _settings.WriteFavoritesAsync(favorites);
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

        return _dialogService.ShowAsync<ConnectingDialog>("Connecting", parameters, options);
    }
}
