using Serilog;
using Microsoft.Extensions.Logging;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        var logger = new LoggerConfiguration().WriteTo.Debug().WriteTo.File(Path.Combine(FileSystem.Current.AppDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day).CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);

        logger.Information("Yes");

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        var app = builder.Build();

        return app;
    }
}
