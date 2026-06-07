using Robust.Launcher.Api.Utility;

namespace Starlight.Launcher.Models;

public sealed class RobustCdn
{
    /// <summary>
    /// Mirror URLs for this single CDN. Treated as interchangeable for
    /// availability (Happy Eyeballs) fallback — they must serve the same content.
    /// </summary>
    public UrlFallbackSet BaseUrl { get; }

    public UrlFallbackSet BuildsManifest => BaseUrl + "manifest.json";
    public UrlFallbackSet ModulesManifest => BaseUrl + "modules.json";

    public RobustCdn(UrlFallbackSet baseUrl)
    {
        BaseUrl = baseUrl;
    }

    public RobustCdn(params string[] mirrorUrls) : this(new UrlFallbackSet([.. mirrorUrls]))
    {
    }
}