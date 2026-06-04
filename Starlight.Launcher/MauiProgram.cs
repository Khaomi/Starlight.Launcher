using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Robust.Launcher.Api.Api;
using Robust.Launcher.Api.Models.ServerStatus;
using Robust.Launcher.Api.Utility;
using Serilog;
using Starlight.Launcher.Services;
using Starlight.Launcher.Services.Auth;
using Starlight.Launcher.Services.EngineManager;
using Starlight.Launcher.Services.Localization;
using Starlight.Launcher.Services.ServerStatus;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;

namespace Starlight.Launcher;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            var builder = MauiApp.CreateBuilder();

            var logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.File(Path.Combine(FileSystem.Current.AppDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day).CreateLogger();

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);
            Log.Logger = logger;

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("CormorantGaramond-Regular.ttf", "Cormorant Garamond");
                });

            try
            {
                var httpClient = HappyEyeballsHttp.CreateHttpClient();
                builder.Services.AddSingleton(httpClient);
                builder.Services.AddSingleton<SettingsService>();
                builder.Services.AddSingleton<AppState>();
                builder.Services.AddSingleton<LocalizationManager>();
                builder.Services.AddTransient<IMauiInitializeService, LocalizationInitializer>();
                builder.Services.AddSingleton<HubApi>();
                builder.Services.AddSingleton<AuthApi>();
                builder.Services.AddSingleton<HubServerFetcher>();
                builder.Services.AddSingleton<ServerStatusCache>();
                builder.Services.AddSingleton<IEngineManager, EngineManagerDynamic>();
                builder.Services.AddSingleton<Updater>();
                builder.Services.AddSingleton<LoginManager>();
                builder.Services.AddTransient<Connector>();
                builder.Services.AddMauiBlazorWebView();
                builder.Services.AddMudServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            var app = builder.Build();

            app.Services.GetRequiredService<HubServerFetcher>().RequestInitialUpdate();
            app.Services.GetRequiredService<LoginManager>().Initialize();

            return app;
        }
        catch (Exception ex)
        {
            var text = ex.ToString();
            System.Diagnostics.Debug.WriteLine(text);
            File.WriteAllText(
                Path.Combine(FileSystem.Current.AppDataDirectory, "startup-crash.txt"), text);
            throw;
        }
    }
}
