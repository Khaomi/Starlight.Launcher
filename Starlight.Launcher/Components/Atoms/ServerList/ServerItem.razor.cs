using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Components.Pages;
using Starlight.Launcher.Services.Localization;

namespace Starlight.Launcher.Components.Atoms.ServerList;

public partial class ServerItem : ComponentBase
{
    [Inject] private LocalizationManager Localization { get; set; } = null!;
    [Parameter, EditorRequired] public ServerStatusData Data { get; set; } = null!;
    [Parameter] public EventCallback<ServerStatusData> OnInfoNeeded { get; set; }
    [Parameter] public EventCallback<ServerStatusData> OnFavorites { get; set; }
    [Parameter] public bool IsInFavorites { get; set; } = false;
    [Inject] private ILogger<ServerItem> _logger { get; set; } = null!;

    private CancellationTokenSource? _infoCts;

    private bool Expanded = false;
    private string RowClass => "server-row server-row--clickable";

    private List<string>? DisplayTags => Data.Tags?
        .Select(t => ParseTag(t))
        .Where(t => !string.IsNullOrEmpty(t))
        .Take(3)
        .ToList();

    private static string ParseTag(string tag)
    {
        if (tag.StartsWith("region:", StringComparison.OrdinalIgnoreCase))
            return Servers.ParseRegionTag(tag);
        if (tag.StartsWith("lang:", StringComparison.OrdinalIgnoreCase))
            return Servers.ParseLangTag(tag);
        if (tag.StartsWith("rp:", StringComparison.OrdinalIgnoreCase))
            return Servers.ParseRPTag(tag);
        return "";
    }

    protected override void OnInitialized()
    {
        Data.Changed += OnDataChanged;

        if (Data.StatusInfo == ServerStatusInfoCode.NotFetched)
        {
            _infoCts = new CancellationTokenSource();
            _ = RequestInfoDebouncedAsync(_infoCts.Token);
        }
    }

    private async Task RequestInfoDebouncedAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);
            await OnInfoNeeded.InvokeAsync(Data);
        }
        catch (OperationCanceledException) { }
    }

    private async void OnDataChanged()
    {
        try { await InvokeAsync(StateHasChanged); }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleClick()
    {
        if (string.IsNullOrEmpty(Data.Description) && Data.StatusInfo is ServerStatusInfoCode.Fetched and not ServerStatusInfoCode.Error)
            _ = RequestInfoDebouncedAsync((_infoCts ?? new CancellationTokenSource()).Token);
        Expanded = !Expanded;
    }

    private async Task HandleFavorites()
    {
        await OnFavorites.InvokeAsync(Data);
    }

    private async Task OnInfoClick(string Url)
    {
        try
        {
            await Browser.OpenAsync(Url, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to open URL {Url}");
        }
    }

    private string? ParseIcon(string? icon)
    {
        if (icon == null)
            return null;

        if (icon == "discord")
            return Icons.Custom.Brands.Discord;

        if (icon == "telegram")
            return Icons.Custom.Brands.Telegram;

        if (icon == "github")
            return Icons.Custom.Brands.GitHub;

        if (icon == "web")
            return Icons.Material.Outlined.Web;

        if (icon == "forum")
            return Icons.Material.Outlined.Forum;

        if (icon == "wiki")
            return Icons.Material.Outlined.Book;

        return icon;
    }

    public void Dispose()
    {
        _infoCts?.Cancel();
        _infoCts?.Dispose();
        Data.Changed -= OnDataChanged;
    }
}