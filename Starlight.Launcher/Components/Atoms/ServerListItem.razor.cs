using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Components.Pages;

namespace Starlight.Launcher.Components.Atoms;

public partial class ServerListItem : ComponentBase
{
    [Parameter, EditorRequired] public ServerStatusData Data { get; set; } = null!;
    [Parameter] public EventCallback<ServerStatusData> OnClick { get; set; }
    [Parameter] public EventCallback<ServerStatusData> OnInfoNeeded { get; set; }
    [Inject] private ILogger<ServerListItem> _logger { get; set; } = null!;

    private CancellationTokenSource? _infoCts;

    private bool Expanded = false;

    private bool IsClickable => OnClick.HasDelegate;
    private string RowClass => IsClickable ? "server-row server-row--clickable" : "server-row";

    private List<string>? DisplayTags => Data.Tags?
        .Where(t => !t.StartsWith("region:", StringComparison.OrdinalIgnoreCase)
                 && !t.StartsWith("lang:", StringComparison.OrdinalIgnoreCase))
        .Select(t => Servers.ParseRPTag(t))
        .Take(3)
        .ToList();

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
        if (IsClickable)
        {
            await OnClick.InvokeAsync(Data);
            Expanded = !Expanded;
        }
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