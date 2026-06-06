using Microsoft.Maui.ApplicationModel;
using Robust.Launcher.Api.Models;
using Serilog;
using Starlight.Launcher.Api.Models;
using Starlight.Launcher.Services.Settings;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Web;

namespace Starlight.Launcher.Services.Auth;

public sealed class DiscordAuthService(StarlightAuthApi api, LoginManager loginManager)
{
    private static readonly TimeSpan FlowTimeout = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();

    public async Task<LoggedInAccount> LoginAsync(CancellationToken cancel = default)
    {
        var state = GenerateState();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[state] = tcs;

        try
        {
            var opened = await Browser.Default.OpenAsync(
                api.BuildLauncherLoginUrl(state), BrowserLaunchMode.SystemPreferred);
            if (!opened)
                throw new DiscordAuthException("Unable to open the browser to log in.");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeoutCts.CancelAfter(FlowTimeout);

            string validationToken;
            await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token)))
            {
                validationToken = await tcs.Task;
            }

            var info = await api.ExchangeValidationTokenAsync(validationToken, cancel);

            loginManager.AddFreshLogin(info);
            loginManager.ActiveAccountId = info.UserId;

            return loginManager.ActiveAccount!;
        }
        finally
        {
            _pending.TryRemove(state, out _);
        }
    }

    public bool HandleDeepLink(Uri uri)
    {
        if (!uri.Scheme.Equals("starlight", StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("auth", StringComparison.OrdinalIgnoreCase))
            return false;

        var query = HttpUtility.ParseQueryString(uri.Query);
        var state = query["state"];

        if (string.IsNullOrEmpty(state) || !_pending.TryRemove(state, out var tcs))
        {
            Log.Warning("Discord deep link with an unknown state");
            return false;
        }

        var error = query["error"];
        if (!string.IsNullOrEmpty(error))
        {
            tcs.TrySetException(new DiscordAuthException(MapError(error)));
            return true;
        }

        var token = query["token"];
        if (string.IsNullOrEmpty(token))
        {
            tcs.TrySetException(new DiscordAuthException("No token in the response."));
            return true;
        }

        tcs.TrySetResult(token);
        return true;
    }

    private static string MapError(string error) => error switch
    {
        "link_required" => "Your Discord account isn't linked to your player. Link it on the website and try again.",
        _ => "Unable to log in via Discord.",
    };

    private static string GenerateState()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}

public sealed class DiscordAuthException(string message) : Exception(message);