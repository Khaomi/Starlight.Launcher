namespace Starlight.Launcher.Models.Data;

public record FavoriteServer(string? Name, string Address)
{
    public string? LastUsedName { get; set; } = null;
    public string Address { get; set; } = Address;
}
