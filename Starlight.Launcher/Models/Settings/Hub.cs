
namespace Starlight.Launcher.Models.Settings;

public record Hub
{
    public required Uri HubUri { get; set; }
    public required long Priority { get; set; }
}