using Microsoft.Extensions.Logging;
using Starlight.Launcher.Services.Settings;
using Starlight.Launcher.Services.State;

namespace Starlight.Launcher.Services.Localization;

public partial class LocalizationInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var localizationManager = services.GetRequiredService<LocalizationManager>();
        var state = services.GetRequiredService<AppState>();
        var settingsService = services.GetRequiredService<SettingsService>();
        var logger = services.GetRequiredService<ILogger<LocalizationManager>>();

        Task.Run(async () => await localizationManager.Initialize(logger, settingsService, state));
    }
}
