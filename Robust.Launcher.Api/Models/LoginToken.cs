using System;

namespace Robust.Launcher.Api.Models;

public readonly struct LoginToken
{
    public readonly string Token;
    public readonly DateTimeOffset ExpireTime;

    public LoginToken(string token, DateTimeOffset expireTime)
    {
        Token = token;
        ExpireTime = expireTime;
    }
}