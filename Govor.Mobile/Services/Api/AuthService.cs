using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Api;

public class AuthService : IAuthService
{
    private bool _isAuthenticated;
    public bool RequiresPasswordUpgrade { get; private set; }

    public event EventHandler<bool> AuthenticationStateChanged;

    private readonly IApiClient _apiClient;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IBuilderDeviceInfoString _deviceInfoString;
    private readonly Govor.Mobile.Services.Interfaces.Notification.IPushNotificationService _push;
    private readonly Govor.Mobile.Services.E2ee.PasswordAccountClient _passwords;

    public AuthService(
        IApiClient apiClient,
        IJwtProviderService jwtProvider,
        IBuilderDeviceInfoString deviceInfoString,
        Govor.Mobile.Services.Interfaces.Notification.IPushNotificationService push,
        Govor.Mobile.Services.E2ee.PasswordAccountClient passwords)
    {
        _apiClient = apiClient;
        _jwtProvider = jwtProvider;
        _deviceInfoString = deviceInfoString;
        _push = push;
        _passwords = passwords;

        _jwtProvider.WasClearTokens += () =>
        {
            IsAuthenticated = false;
        };
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (_isAuthenticated == value)
                return;

            _isAuthenticated = value;
            AuthenticationStateChanged?.Invoke(this, _isAuthenticated);
        }
    }

    public async Task<Result<UserLogin>> LoginAsync(string username, string password)
    {
        RequiresPasswordUpgrade = false;
        try
        {
            var result = await _passwords.LoginAsync(username, password, _deviceInfoString.Info);
            await _jwtProvider.InitializeWithTokensAsync(result.accessToken, result.refreshToken);
            IsAuthenticated = true;
            return Result<UserLogin>.Success(new UserLogin(username, string.Empty, result.refreshToken));
        }
        catch (Exception ex)
        {
            RequiresPasswordUpgrade = ex is Govor.Mobile.Services.E2ee.PasswordUpgradeRequiredException;
            return Result<UserLogin>.Failure(ex.Message);
        }
    }

    public async Task<Result<UserLogin>> RegisterAsync(string username, string password, string invitation)
    {
        try
        {
            var result = await _passwords.RegisterAsync(username, password, invitation, _deviceInfoString.Info);
            await _jwtProvider.InitializeWithTokensAsync(result.accessToken, result.refreshToken);
            IsAuthenticated = true;
            return Result<UserLogin>.Success(new UserLogin(username, string.Empty, result.refreshToken));
        }
        catch (Exception ex) { return Result<UserLogin>.Failure(ex.Message); }
    }

    public async Task<Result<UserLogin>> UpgradeAfterPasswordResetAsync(string username, string resetPassword)
    {
        try
        {
            var result = await _passwords.UpgradeAfterPasswordResetAsync(username, resetPassword, _deviceInfoString.Info);
            await _jwtProvider.InitializeWithTokensAsync(result.accessToken, result.refreshToken);
            IsAuthenticated = true;
            return Result<UserLogin>.Success(new UserLogin(username, string.Empty, result.refreshToken));
        }
        catch (Exception ex) { return Result<UserLogin>.Failure(ex.Message); }
    }
    public async Task LogoutAsync()
    {
        // DELETE must run while the session still has usable credentials.
        try { await _push.UnregisterAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Push logout cleanup failed: {ex.Message}"); }
        var result = await _apiClient.DeleteAsync("api/session/close");
        if (result.IsSuccess)
        {
            await _jwtProvider.ClearAsync();
            IsAuthenticated = false;
        }
        else
        {
            await _jwtProvider.ClearAsync();
            IsAuthenticated = false;
            throw new LogoutException(result.ErrorMessage ?? "Something happened");
        }
    }

    public async Task InitializeAsync()
    {
        await _jwtProvider.InitializeAsync();
        // Migrate installations that previously stored only the refresh token.
        if (_jwtProvider.HasRefreshToken && _jwtProvider.CurrentUserId is null)
            await _jwtProvider.GetAccessTokenAsync();
        _isAuthenticated = _jwtProvider.HasRefreshToken;
    }
}
public class LogoutException(string message) : Exception(message);
