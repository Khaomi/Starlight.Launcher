using Microsoft.AspNetCore.Components;
using MudBlazor;
using Starlight.Launcher.Components.Atoms.Settings;
using Starlight.Launcher.Models.Settings;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;
using System.Globalization;

namespace Starlight.Launcher.Components.Pages;

public partial class Settings : ComponentBase, IDisposable
{
    [Inject] private SettingsService Service { get; set; } = null!;
    [Inject] private LocalizationManager Localization { get; set; } = null!;
    [Inject] private AppState State { get; set; } = null!;
    [Inject] private IDialogService Dialog { get; set; } = null!;
    private List<string> AvailableLanguages = new();

    private MudTabs tabs = null!;

    private MudTabPanel settingsTab = null!;

    protected override async Task OnInitializedAsync()
    {
        var settings = await Service.GetSettingsAsync();
        AvailableLanguages = Localization.EnumarateAllLoadedLanguages().Select(x => new CultureInfo(x).Name).ToList();
        State.OnChange += OnStateChanged;
        await base.OnInitializedAsync();
    }

    private void OnStateChanged()
    {
        StateHasChanged();
    }

    private void OnActivePanelIndexChanged(int value)
    {
        if (value == 2)
        {
            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = false,
            };
            Task.Run(async () => {
                var dialog = await Dialog.ShowAsync<AlertDialog>("Development tab alert", options);
                if (dialog.Dialog is AlertDialog alert)
                {
                    alert.OnCancel += async () =>
                    {
                        await tabs.ActivatePanelAsync(settingsTab);
                    };
                }
            });
        }
    }

    private Task OnLanguageChanged(string? value, Action<string?>? setLocal,
        Func<AppSettings, string?, AppSettings> update)
    {
        if (value is not null && AvailableLanguages.Contains(value))
            Localization.SwitchLanguage(value?.ToString() ?? string.Empty);

        setLocal?.Invoke(value);
        return UpdateSetting(s => update(s, value));
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

    private Task OnEnumSettingChanged(int value, Action<int>? setLocal,
        Func<AppSettings, int, AppSettings> update)
    {
        setLocal?.Invoke(value);
        return UpdateSetting(s => update(s, value));
    }

    private async Task UpdateSetting(Func<AppSettings, AppSettings> update)
    {
        var settings = await Service.GetSettingsAsync();
        var newSettings = update(settings);
        await Service.WriteSettingsAsync(newSettings);
        State.CallUpdate();
    }

    private async Task<T> FetchSettings<T>(Func<AppSettings, T> func)
    {
        var settings = await Service.GetSettingsAsync();
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

    public void Dispose()
    {
        State.OnChange -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}
