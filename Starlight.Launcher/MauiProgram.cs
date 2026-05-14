using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Robust.Launcher.Api.Api;
using Robust.Launcher.Api.Models.ServerStatus;
using Robust.Launcher.Api.Utility;
using Serilog;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;

namespace Starlight.Launcher;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        var logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.File(Path.Combine(FileSystem.Current.AppDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day).CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("CormorantGaramond-Regular.ttf", "Cormorant Garamond");
            });

        var httpClient = HappyEyeballsHttp.CreateHttpClient();
        builder.Services.AddSingleton(httpClient);
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<LocalizationManager>();
        builder.Services.AddTransient<IMauiInitializeService, LocalizationInitializer>();
        builder.Services.AddSingleton<HubApi>();
        builder.Services.AddSingleton<HubServerFetcher>();
        builder.Services.AddSingleton<ServerStatusCache>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        var app = builder.Build();

        app.Services.GetRequiredService<HubServerFetcher>().RequestInitialUpdate();

        return app;
    }
}
