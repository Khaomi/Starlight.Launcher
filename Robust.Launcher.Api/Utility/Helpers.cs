using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Robust.Launcher.Api.Utility;

public static class Helpers
{
    public static readonly JsonSerializerOptions JsonWebOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T> AsJson<T>(this HttpContent content) where T : notnull
    {
        var str = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(str, JsonWebOptions) ??
               throw new JsonException("AsJson: did not expect null response");
    }
}