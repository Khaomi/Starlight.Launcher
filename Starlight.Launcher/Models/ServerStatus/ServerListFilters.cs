namespace Starlight.Launcher.Models.ServerStatus;

public sealed class ServerListFilters
{
    public string SearchQuery { get; set; } = "";
    public HashSet<string> SelectedTags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Region { get; set; }
    public bool HideEmpty { get; set; }
    public bool HideFull { get; set; }
    public ServerSortMode SortBy { get; set; } = ServerSortMode.Players;

    public event Action? Changed;
    public void NotifyChanged() => Changed?.Invoke();
}

public enum ServerSortMode
{
    Players,
    Name,
    Ping,
}