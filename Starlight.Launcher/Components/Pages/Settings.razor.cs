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
    private List<string> AvailableLanguages = [];

    private MudTabs tabs = null!;

    private MudTabPanel settingsTab = null!;

    private AppSettings? appSettingsCache = null;
    private DateTime LastCacheUpdate;
    private readonly TimeSpan CacheUpdateInterval = TimeSpan.FromSeconds(2);

    protected override async Task OnInitializedAsync()
    {
        var settings = await Service.GetSettingsAsync();
        AvailableLanguages = Localization.EnumarateAllLoadedLanguages().Select(x => new CultureInfo(x).Name).ToList();
        State.OnChange += OnStateChanged;
        await base.OnInitializedAsync();
    }

    private void OnStateChanged() 
        => StateHasChanged();

    private async void OnActivePanelIndexChanged(int value)
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

            var dialog = await Dialog.ShowAsync<AlertDialog>("Development tab alert", options);
            if (dialog.Dialog is AlertDialog alert)
            {
                alert.OnCancel += async () =>
                {
                    await tabs.ActivatePanelAsync(settingsTab);
                };
            }
        }
    }

    private Task OnLanguageChanged(string? value, Action<string?>? setLocal,
        Func<AppSettings, string?, AppSettings> update)
    {
        if (value is not null && AvailableLanguages.Contains(value))
            Localization.SwitchLanguage(value?.ToString() ?? string.Empty);

        setLocal?.Invoke(value);
        return UpdateSetting(s => update(s, value), true);
    }

    private Task OnSettingChanged<T>(T value, Action<T>? setLocal, Func<AppSettings, T, AppSettings> update, bool callWindowUpdate = false)
    {
        setLocal?.Invoke(value);
        return UpdateSetting(s => update(s, value), callWindowUpdate);
    }

    private async Task UpdateSetting(Func<AppSettings, AppSettings> update, bool callWindowUpdate = false)
    {
        var settings = await Service.GetSettingsAsync();
        var newSettings = update(settings);
        await Service.WriteSettingsAsync(newSettings);
        if (callWindowUpdate)
            State.CallUpdate();
    }

    private async Task<T> FetchSettings<T>(Func<AppSettings, T> func)
    {
        AppSettings settings;
        if (appSettingsCache != null &&
            DateTime.Now - LastCacheUpdate < CacheUpdateInterval)
        {
            settings = appSettingsCache;
        }
        else
        {
            settings = await Service.GetSettingsAsync();
            appSettingsCache = settings;
            LastCacheUpdate = DateTime.Now;
        }

        var result = func(settings);
        return result;
    }

    private static List<Hub> ConvertToHubList(List<(string Url, long Priority)> list)
    {
        List<Hub> hubUris = [];
        foreach (var (Url, Priority) in list)
            if (Uri.TryCreate(Url, UriKind.Absolute, out var uri))
                hubUris.Add(new Hub { HubUri = uri, Priority = Priority });

        return hubUris;
    }

    public void Dispose()
    {
        State.OnChange -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}
