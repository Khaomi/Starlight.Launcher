using System.Text.Json.Serialization;

namespace Robust.Launcher.Api.Models.Data;

public sealed record InstalledEngineVersion(
    [property: JsonPropertyName("version")]
    string Version,
    [property: JsonPropertyName("signature")]
    string Signature);