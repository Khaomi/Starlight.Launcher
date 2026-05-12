using Microsoft.AspNetCore.Components;
using Starlight.Launcher.Models.Settings;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;
using System.Globalization;

namespace Starlight.Launcher.Components.Pages;

public partial class Settings : ComponentBase
{
    [Inject] private SettingsService _settings { get; set; } = null!;
    [Inject] private LocalizationManager _localization { get; set; } = null!;
    [Inject] private AppState _state { get; set; } = null!;
    private List<(string Url, long Priority)> Hubs = new();
    private List<string> AvailableLanguages = new();

    protected override async Task OnInitializedAsync()
    {
        var settings = await _settings.GetSettingsAsync();
        AvailableLanguages = _localization.EnumarateAllLoadedLanguages().Select(x => new CultureInfo(x).Name).ToList();
        Hubs = [.. settings.Hubs.Select(h => (h.HubUri.ToString(), h.Priority))];
    }

    private Task OnBoolSettingChanged(
        bool value,
        Action<bool>? setLocal,
        Func<AppSettings, bool, AppSettings> update)
    {
        setLocal?.Invoke(value);

        return UpdateSetting(s => update(s, value));
    }

    private Task OnListSettingChanged<T>(List<T> value, Action<List<T>>? setLocal,
        Func<AppSettings, List<T>, AppSettings> update)
    {
        setLocal?.Invoke(value);

        return UpdateSetting(s => update(s, value));
    }

    private Task OnListSettingChanged<T>(T value, Action<T>? setLocal,
        Func<AppSettings, T, AppSettings> update)
    {
        setLocal?.Invoke(value);

        return UpdateSetting(s => update(s, value));
    }

    private async Task UpdateSetting(Func<AppSettings, AppSettings> update)
    {
        var settings = await _settings.GetSettingsAsync();
        var newSettings = update(settings);
        await _settings.WriteSettingsAsync(newSettings);
        _state.CallUpdate();
    }

    private async Task<T> FetchSettings<T>(Func<AppSettings, T> func)
    {
        var settings = await _settings.GetSettingsAsync();
        var result = func(settings);
        return result;
    }

    private List<Hub> ConvertToHubList(List<(string Url, long Priority)> list)
    {
        List<Hub> hubUris = [];
        foreach (var hub in list)
        {
            if (Uri.TryCreate(hub.Url, UriKind.Absolute, out var uri))
                hubUris.Add(new Hub { HubUri = uri, Priority = hub.Priority });
        }

        return hubUris;
    }
}
