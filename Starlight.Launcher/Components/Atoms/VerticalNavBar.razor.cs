using Microsoft.AspNetCore.Components;
using Starlight.Launcher.Services.Localization;

namespace Starlight.Launcher.Components.Atoms;

public sealed partial class VerticalNavBar : ComponentBase
{
    [Inject] private LocalizationManager Localization { get; set; } = null!;
}