namespace Starlight.Launcher.Models.Data;

/// <summary>
/// Index of all available localizations contained in json files in "Resources/Localization" directory.
/// Key is localization name, value is paths to ftl files with localization data.
/// </summary>
public sealed class LocalizationsManifest : Dictionary<string, List<string>>;