using Microsoft.AspNetCore.Components;

namespace Starlight.Launcher.Components.Atoms;

public partial class SettingsStringListOption : ComponentBase
{
    [Parameter] public List<(string Url, long Priority)> Value { get; set; } = [];
    [Parameter] public EventCallback<List<(string Url, long Priority)>> ValueChanged { get; set; }
    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string Description { get; set; } = default!;
    [Parameter] public string Icon { get; set; } = default!;

    private async Task OnAddButtonPressed()
    {
        // 0 = highest priority; new item goes to the end (lowest priority).
        Value.Add((string.Empty, Value.Count));
        await NotifyChanged();
    }

    private async Task RemoveAt(int index)
    {
        if (index < 0 || index >= Value.Count)
            return;

        Value.RemoveAt(index);
        ReassignPriorities();
        await NotifyChanged();
    }

    private async Task MoveUp(int index)
    {
        if (index <= 0 || index >= Value.Count)
            return;

        (Value[index - 1], Value[index]) = (Value[index], Value[index - 1]);
        ReassignPriorities();
        await NotifyChanged();
    }

    private async Task MoveDown(int index)
    {
        if (index < 0 || index >= Value.Count - 1)
            return;

        (Value[index + 1], Value[index]) = (Value[index], Value[index + 1]);
        ReassignPriorities();
        await NotifyChanged();
    }

    private async Task OnValueChanged(string value, int index)
    {
        if (index < 0 || index >= Value.Count)
            return;

        Value[index] = (value, Value[index].Priority);
        await NotifyChanged();
    }

    private void ReassignPriorities()
    {
        for (var i = 0; i < Value.Count; i++)
            Value[i] = (Value[i].Url, i);
    }

    private Task NotifyChanged() => ValueChanged.InvokeAsync(Value);

    private string? ValidateUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "URL is required";

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return "Invalid URL";

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return "Only HTTP/HTTPS allowed";

        return null;
    }
}