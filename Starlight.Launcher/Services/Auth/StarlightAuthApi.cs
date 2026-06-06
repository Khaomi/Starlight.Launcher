using System.Net.Http.Json;
using Robust.Launcher.Api.Models;
using Robust.Launcher.Api.Models.Data;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Services.Auth;

public sealed class StarlightAuthApi(HttpClient http, SettingsService settings)
{
    public string BuildLauncherLoginUrl(string state)
        => new Uri(new Uri(settings.GetSettings().StarlightAPIUrl), $"api/discord-auth/launcher-login?state={Uri.EscapeDataString(state)}").ToString();

    public async Task<LoginInfo> ExchangeValidationTokenAsync(string validationToken, CancellationToken cancel)
    {
        using var resp = await http.PostAsJsonAsync(
            "token/validate",
            new { token = validationToken },
            cancel);

        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<ValidateResponse>(cancellationToken: cancel)
                   ?? throw new DiscordAuthException("Empty response when exchanging token.");

        return new LoginInfo
        {
            UserId = body.UserId,
            Username = body.Username,
            Token = new LoginToken() { Token = body.Token, ExpireTime = body.ExpireTime },
        };
    }

    private sealed record ValidateResponse(Guid UserId, string Username, string Token, DateTimeOffset ExpireTime);
}