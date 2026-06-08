using Robust.Launcher.Api.Models;
using Robust.Launcher.Api.Models.Data;
using Starlight.Launcher.Models.Helpers;

namespace Starlight.Launcher.Api.Models;

public abstract class LoggedInAccount : ObservableObject
{
    public string Username => LoginInfo.Username;
    public Guid UserId => LoginInfo.UserId;

    protected LoggedInAccount(LoginInfo loginInfo) => LoginInfo = loginInfo;

    public LoginInfo LoginInfo { get; }

    public abstract AccountLoginStatus Status { get; }
}
