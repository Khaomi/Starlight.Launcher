using System.Net.Http.Headers;
using static Robust.Launcher.Api.Utility.HttpUtility;

namespace Starlight.Launcher.Utility;

public static class HttpUtility
{
    private static readonly StringWithQualityHeaderValue ZStdHeader = new StringWithQualityHeaderValue("zstd", 1);

    public static async Task<HttpResponseMessage> SendZStdAsync(
        this HttpClient client,
        HttpRequestMessage message,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancel = default)
    {
        message.Headers.AcceptEncoding.Add(ZStdHeader);

        var response = await client.SendAsync(message, completionOption, cancel);

        if (response.Content.Headers.ContentEncoding.LastOrDefault() == "zstd")
        {
            response.Content = new ZStdHttpContent(response.Content);
        }

        return response;
    }

    public sealed class ZStdHttpContent : DecompressedContent
    {
        public ZStdHttpContent(HttpContent originalContent) : base(originalContent)
        {
        }

        protected override Stream GetDecompressedStream(Stream originalStream)
        {
            return new ZStdDecompressStream(originalStream);
        }
    }
}