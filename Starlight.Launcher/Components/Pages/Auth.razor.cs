using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Robust.Launcher.Api.Api;
using Robust.Launcher.Api.Models;
using Starlight.Launcher.Api.Models;

namespace Starlight.Launcher.Components.Pages;

public partial class Auth : Microsoft.AspNetCore.Components.ComponentBase
{
    private enum Mode
    {
        AccountList,
        SignIn,
        Register,
        ForgotPassword
    }

    private Mode _mode = Mode.AccountList;
    private bool _busy;

    private string _signInUsername = "";
    private string _signInPassword = "";
    private string _signInTfaCode = "";
    private bool _signInTfaRequired;
    private string? _signInError;
    private bool _signInShowResend;

    private string _registerUsername = "";
    private string _registerEmail = "";
    private string _registerPassword = "";
    private string _registerPasswordConfirm = "";
    private string[]? _registerErrors;
    private string? _registerSuccessMessage;

    private string _forgotEmail = "";
    private string? _forgotError;
    private bool _forgotSuccess;

    private Guid? _relogUserId;

    protected override void OnInitialized()
    {
        LoginManager.LoginsChanged += OnLoginsChanged;

        if (LoginManager.Logins.Count == 0)
            _mode = Mode.SignIn;
    }

    private void OnLoginsChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        LoginManager.LoginsChanged -= OnLoginsChanged;
    }

    private async Task SelectAccount(LoggedInAccount account)
    {
        _busy = true;
        try
        {
            try
            {
                await LoginManager.UpdateSingleAccountStatus(account);
            }
            catch (AuthApiException ex)
            {
                Snackbar.Add($"The token could not be verified: {ex.Message}", Severity.Warning);
            }

            if (account.Status == AccountLoginStatus.Expired)
            {
                Snackbar.Add("Your session has expired. Please log in again.", Severity.Warning);
                BeginRelogin(account);
                return;
            }

            LoginManager.ActiveAccountId = account.UserId;
            Nav.NavigateTo("/");
        }
        finally
        {
            _busy = false;
        }
    }

    private void RemoveAccount(LoggedInAccount account)
    {
        LoginManager.RemoveLogin(account.UserId);
        Snackbar.Add($"Account {account.LoginInfo.Username} deleted", Severity.Info);
    }

    private void GoToSignIn()
    {
        ResetSignInForm();
        _mode = Mode.SignIn;
    }

    private void BeginRelogin(LoggedInAccount account)
    {
        ResetSignInForm();
        _relogUserId = account.UserId;
        _signInUsername = account.LoginInfo.Username;
        _mode = Mode.SignIn;
    }

    private static string StatusLabel(AccountLoginStatus s) => s switch
    {
        AccountLoginStatus.Available => "Online",
        AccountLoginStatus.Expired => "The session has expired",
        AccountLoginStatus.Unsure => "Checking...",
        _ => s.ToString()
    };

    private async Task OnSignInKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_busy)
            await DoSignIn();
    }

    private async Task DoSignIn()
    {
        _signInError = null;
        _signInShowResend = false;

        if (string.IsNullOrWhiteSpace(_signInUsername) || string.IsNullOrEmpty(_signInPassword))
        {
            _signInError = "Enter your username and password";
            return;
        }

        _busy = true;
        try
        {
            AuthApi.AuthenticateRequest request;
            if (_relogUserId.HasValue)
            {
                request = new AuthApi.AuthenticateRequest(
                    null, _relogUserId.Value, _signInPassword,
                    _signInTfaRequired ? _signInTfaCode : null);
            }
            else
            {
                request = new AuthApi.AuthenticateRequest(
                    _signInUsername, null, _signInPassword,
                    _signInTfaRequired ? _signInTfaCode : null);
            }

            var result = await AuthApi.AuthenticateAsync(request);

            if (result.IsSuccess)
            {
                LoginManager.AddFreshLogin(result.LoginInfo);
                LoginManager.ActiveAccountId = result.LoginInfo.UserId;
                Snackbar.Add($"Welcome, {result.LoginInfo.Username}!", Severity.Success);
                Nav.NavigateTo("/");
                return;
            }

            switch (result.Code)
            {
                case AuthApi.AuthenticateDenyResponseCode.InvalidCredentials:
                    _signInError = "Incorrect username or password";
                    break;

                case AuthApi.AuthenticateDenyResponseCode.AccountUnconfirmed:
                    _signInError = "Your account has not been verified. Please check your email.";
                    _signInShowResend = true;
                    break;

                case AuthApi.AuthenticateDenyResponseCode.TfaRequired:
                    _signInTfaRequired = true;
                    _signInError = "Enter your two-factor authentication code";
                    break;

                case AuthApi.AuthenticateDenyResponseCode.TfaInvalid:
                    _signInTfaRequired = true;
                    _signInError = "Invalid 2FA code";
                    break;

                case AuthApi.AuthenticateDenyResponseCode.AccountLocked:
                    _signInError = "Account blocked";
                    break;

                default:
                    _signInError = string.Join("\n", result.Errors);
                    break;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResendConfirmation()
    {
        _busy = true;
        try
        {
            if (_signInUsername.Contains('@'))
            {
                var errors = await AuthApi.ResendConfirmationAsync(_signInUsername);
                if (errors == null)
                    Snackbar.Add("The email has been resent", Severity.Success);
                else
                    Snackbar.Add(string.Join("\n", errors), Severity.Error);
            }
            else
            {
                Snackbar.Add("To resend, enter your email address in the username field", Severity.Warning);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void ResetSignInForm()
    {
        _signInUsername = "";
        _signInPassword = "";
        _signInTfaCode = "";
        _signInTfaRequired = false;
        _signInError = null;
        _signInShowResend = false;
        _relogUserId = null;
    }

    private void BackToAccountList()
    {
        ResetSignInForm();
        _mode = Mode.AccountList;
    }

    private async Task DoRegister()
    {
        _registerErrors = null;
        _registerSuccessMessage = null;

        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(_registerUsername))
            validationErrors.Add("Enter your username");
        if (string.IsNullOrWhiteSpace(_registerEmail) || !_registerEmail.Contains('@'))
            validationErrors.Add("Please enter a valid email address");
        if (_registerPassword.Length < 8)
            validationErrors.Add("The password must be at least 8 characters long");
        if (_registerPassword != _registerPasswordConfirm)
            validationErrors.Add("The passwords do not match");

        if (validationErrors.Count > 0)
        {
            _registerErrors = validationErrors.ToArray();
            return;
        }

        _busy = true;
        try
        {
            var result = await AuthApi.RegisterAsync(_registerUsername, _registerEmail, _registerPassword);

            if (!result.IsSuccess)
            {
                _registerErrors = result.Errors;
                return;
            }

            _registerSuccessMessage = result.Status switch
            {
                RegisterResponseStatus.Registered =>
                    "Registration was successful! You can now log in.",
                RegisterResponseStatus.RegisteredNeedConfirmation =>
                    $"Registration was successful! An email has been sent to {_registerEmail} to confirm your account.",
                _ => "Registration successful!"
            };

            _registerPassword = "";
            _registerPasswordConfirm = "";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DoForgotPassword()
    {
        _forgotError = null;

        if (string.IsNullOrWhiteSpace(_forgotEmail) || !_forgotEmail.Contains('@'))
        {
            _forgotError = "Enter valid email";
            return;
        }

        _busy = true;
        try
        {
            var errors = await AuthApi.ForgotPasswordAsync(_forgotEmail);
            if (errors == null)
            {
                _forgotSuccess = true;
            }
            else
            {
                _forgotError = string.Join("\n", errors);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void SwitchMode(Mode mode)
    {
        if (mode == Mode.SignIn) ResetSignInForm();
        if (mode == Mode.Register) { _registerErrors = null; _registerSuccessMessage = null; }
        if (mode == Mode.ForgotPassword) { _forgotError = null; _forgotSuccess = false; }

        _mode = mode;
    }
}