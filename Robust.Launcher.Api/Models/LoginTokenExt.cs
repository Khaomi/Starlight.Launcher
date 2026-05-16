using System;

namespace Robust.Launcher.Api.Models;

public static class LoginTokenExt
{
    public static bool IsTimeExpired(this LoginToken token)
    {
        return token.ExpireTime <= DateTimeOffset.UtcNow;
    }

    public static bool ShouldRefresh(this LoginToken token)
    {
        return token.ExpireTime <= DateTimeOffset.UtcNow + TimeSpan.FromDays(7);
    }
}