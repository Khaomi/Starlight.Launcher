using Microsoft.Extensions.Logging;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Services.Localization;

public partial class LocalizationInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var localizationManager = services.GetRequiredService<LocalizationManager>();
        var settingsService = services.GetRequiredService<SettingsService>();
        var logger = services.GetRequiredService<ILogger<LocalizationManager>>();

        Task.Run(async () => await localizationManager.Initialize(logger, settingsService));
    }
}
