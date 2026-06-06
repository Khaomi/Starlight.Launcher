using System;

namespace Robust.Launcher.Api.Models;

public sealed class LoginToken
{
    public string Token { get; set; } = "";
    public DateTimeOffset ExpireTime { get; set; }
}