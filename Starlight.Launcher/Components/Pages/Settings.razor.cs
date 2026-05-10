using Microsoft.AspNetCore.Components;
using Starlight.Launcher.Models.Settings;

namespace Starlight.Launcher.Components.Pages;

public partial class Settings : ComponentBase
{
    private List<(string Url, long Priority)> Hubs = new();

    protected override async Task OnInitializedAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        Hubs = [.. settings.Hubs.Select(h => (h.HubUri.ToString(), h.Priority))];
    }

    private async Task OnHubsChanged(List<(string Url, long Priority)> value)
    {
        List<Hub> hubUris = [];
        foreach (var hub in value)
        {
            if (Uri.TryCreate(hub.Url, UriKind.Absolute, out var uri))
                hubUris.Add(new Hub { HubUri = uri, Priority = hub.Priority });
        }
        await OnListSettingChanged(value, v => Hubs = v, (s, v) => s with { Hubs = hubUris });
    }

    private Task OnBoolSettingChanged(
        bool value,
        Action<bool>? setLocal,
        Func<AppSettings, bool, AppSettings> update)
    {
        setLocal?.Invoke(value);

        return UpdateSetting(s => update(s, value));
    }

    private Task OnListSettingChanged<T>(List<T> value, Action<List<T>> setLocal,
        Func<AppSettings, List<T>, AppSettings> update)
    {
        setLocal(value);
        return UpdateSetting(s => update(s, value));
    }

    private async Task UpdateSetting(Func<AppSettings, AppSettings> update)
    {
        var settings = await _settings.GetSettingsAsync();
        var newSettings = update(settings);
        await _settings.WriteSettingsAsync(newSettings);
        _state.CallUpdate();
    }

    private async Task<bool> FetchSettings(Func<AppSettings, bool> func)
    {
        var settings = await _settings.GetSettingsAsync();
        var result = func(settings);
        return result;
    }
}
