using System;
using System.Net;

namespace Robust.Launcher.Api.Api;

public sealed class HubApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public TimeSpan? RetryAfter { get; }
    public string? RequestUrl { get; }
    public bool IsRateLimited => StatusCode == HttpStatusCode.TooManyRequests;
    public bool IsTimeout { get; }

    public HubApiException(
        string message,
        HttpStatusCode? statusCode = null,
        TimeSpan? retryAfter = null,
        string? requestUrl = null,
        bool isTimeout = false,
        Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
        RequestUrl = requestUrl;
        IsTimeout = isTimeout;
    }
}