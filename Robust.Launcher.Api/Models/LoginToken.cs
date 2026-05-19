using System;

namespace Robust.Launcher.Api.Models;

public readonly struct LoginToken
{
    public string Token { get; }
    public DateTimeOffset ExpireTime { get; }

    public LoginToken(string token, DateTimeOffset expireTime)
    {
        Token = token;
        ExpireTime = expireTime;
    }
}