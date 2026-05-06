using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Starlight.Launcher.Components.Atoms;

public partial class SettingsBoolOption : ComponentBase
{
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string Description { get; set; } = default!;
    [Parameter] public string Icon { get; set; } = default!;

    private Task OnValueChanged(bool value)
        => ValueChanged.InvokeAsync(value);
}