using System;

namespace Robust.Launcher.Api.Models.Data;

public class LoginInfo
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
    public LoginToken Token { get; set; } = default!;

    public override string ToString()
        => $"{Username}/{UserId}";
}