using System.Text.Json;

var localeRoot = args[0];
var outputFile = args[1];

var result = new Dictionary<string, string[]>();

foreach (var dir in Directory.GetDirectories(localeRoot))
{
    var localeId = Path.GetFileName(dir);

    var files = Directory.GetFiles(dir, "*.ftl", SearchOption.AllDirectories)
        .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/'))
        .OrderBy(x => x)
        .ToArray();

    if (files.Length > 0)
        result[localeId] = files;
}

await File.WriteAllTextAsync(
    outputFile,
    JsonSerializer.Serialize(result, new JsonSerializerOptions
    {
        WriteIndented = true
    }));