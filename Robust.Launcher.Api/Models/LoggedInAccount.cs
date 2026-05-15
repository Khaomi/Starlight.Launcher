using Robust.Launcher.Api.Models.Data;
using System;

namespace Robust.Launcher.Api.Models;

public abstract class LoggedInAccount
{
    public string Username => LoginInfo.Username;
    public Guid UserId => LoginInfo.UserId;

    protected LoggedInAccount(LoginInfo loginInfo)
    {
        LoginInfo = loginInfo;
    }

    public LoginInfo LoginInfo { get; }

    public abstract AccountLoginStatus Status { get; }
}