using System.Net.Http.Json;
using Robust.Launcher.Api.Models;
using Robust.Launcher.Api.Models.Data;
using Starlight.Launcher.Services.Settings;

namespace Starlight.Launcher.Services.Auth;

public sealed class StarlightAuthApi(HttpClient http, SettingsService settings)
{
    public string BuildLauncherLoginUrl(string state)
        => new Uri(new Uri(settings.GetSettings().StarlightAPIUrl), $"api/discord-auth/launcher-login?state={Uri.EscapeDataString(state)}").ToString();

    public async Task<DiscordUserResponse> GetDiscordUserAsync(
        string discordToken,
        CancellationToken cancel)
    {
        var resp = await http.GetAsync(new Uri(new Uri(settings.GetSettings().StarlightAPIUrl), $"api/discord-auth/find-user?token={Uri.EscapeDataString(discordToken)}"), cancel);

        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadFromJsonAsync<DiscordUserResponse>(
                   cancellationToken: cancel)
               ?? throw new DiscordAuthException("Empty response.");
    }
}
public sealed record DiscordUserResponse(Guid? UserId, string Username);