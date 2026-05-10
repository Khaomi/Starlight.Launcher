using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using MudBlazor;
using Robust.Launcher.Api.Models.ServerStatus;
using Starlight.Launcher.Components.Pages;
using static ObjCRuntime.Dlfcn;
using static System.Net.WebRequestMethods;

namespace Starlight.Launcher.Components.Atoms;

public partial class ServerListItem : ComponentBase
{
    [Parameter, EditorRequired] public ServerStatusData Data { get; set; } = null!;
    [Parameter] public EventCallback<ServerStatusData> OnClick { get; set; }
    [Parameter] public EventCallback<ServerStatusData> OnInfoNeeded { get; set; }

    private CancellationTokenSource? _infoCts;

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
            await OnClick.InvokeAsync(Data);
    }

    public void Dispose()
    {
        _infoCts?.Cancel();
        _infoCts?.Dispose();
        Data.Changed -= OnDataChanged;
    }
}