using System.Net;
using System.Net.Http.Json;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Services.Auth;

public sealed class StarlightAuthApi(HttpClient http, SettingsService settings)
{
    public readonly Uri apiUrl = new(settings.GetSettings().StarlightAPIUrl);

    public string BuildLauncherLoginUrl(string state)
        => new Uri(apiUrl, $"api/discord-auth/launcher-login?state={Uri.EscapeDataString(state)}").ToString();

    public async Task<DiscordUserResponse> GetDiscordUserAsync(
        string discordToken,
        CancellationToken cancel)
    {
        var resp = await http.GetAsync(new Uri(apiUrl, $"api/discord-auth/find-user?token={Uri.EscapeDataString(discordToken)}"), cancel);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancel);
            throw new DiscordAuthException($"find-user failed: {(int)resp.StatusCode} {body}");
        }

        return await resp.Content.ReadFromJsonAsync<DiscordUserResponse>(
                   cancellationToken: cancel)
               ?? throw new DiscordAuthException("Empty response.");
    }

    public async Task<bool> ValidateDiscordToken(string discordToken)
    {
        var resp = await http.GetAsync(new Uri(apiUrl, $"api/discord-auth/validate?token={Uri.EscapeDataString(discordToken)}"));

        return resp.IsSuccessStatusCode;
    }

    public async Task<StarlightRefreshResult?> RefreshTokenAsync(
        string sessionId, string refreshToken, CancellationToken cancel = default)
    {
        using var resp = await http.PostAsJsonAsync(new Uri(apiUrl, "/token/refresh"), new { sessionId, refreshToken }, cancel);

        if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            return null; // expired / revoked / reuse_detected -> re-login

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<StarlightRefreshResult>(cancellationToken: cancel);
    }
}

public sealed record StarlightRefreshResult(
    string AccessToken, DateTime AccessExpiresUtc, string RefreshToken, string SessionId);

public sealed record DiscordUserResponse(Guid UserId, string Username);