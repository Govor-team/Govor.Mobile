using Govor.Mobile.Application.Models.Requests;
using Govor.Mobile.Application.Models.Responses;
using Govor.Mobile.Application.Models.Results;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Services.Interfaces;
using Govor.Mobile.Application.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Application.Services.Api;

public class AuthService : IAuthService
{
    private bool _isAuthenticated;

    public event EventHandler<bool> AuthenticationStateChanged;

    private readonly IApiClient _apiClient;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IBuilderDeviceInfoString _deviceInfoString;

    public AuthService(
        IApiClient apiClient,
        IJwtProviderService jwtProvider,
        IBuilderDeviceInfoString deviceInfoString)
    {
        _apiClient = apiClient;
        _jwtProvider = jwtProvider;
        _deviceInfoString = deviceInfoString;
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
        var result = await _apiClient.PostAsync<AuthResponse>("/api/auth/login",
           new LoingRequest(username, password, _deviceInfoString.Info));

        if (result.IsSuccess)
        {
            await _jwtProvider.SetTokensAsync(result.Value.accessToken,
                result.Value.refreshToken);

            IsAuthenticated = true;

            return Result<UserLogin>.Success(
                    new UserLogin(username, password, result.Value.refreshToken)
                );

        }
        else
        {
            return Result<UserLogin>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<UserLogin>> RegisterAsync(string username, string password, string invitation)
    {
        var result = await _apiClient.PostAsync<AuthResponse>("api/auth/register",
            new RegisterRequest(username, password, invitation, _deviceInfoString.Info));

        if (result.IsSuccess)
        {
            await _jwtProvider.SetTokensAsync(result.Value.accessToken,
                result.Value.refreshToken);

            IsAuthenticated = true;

            return Result<UserLogin>.Success(
                    new UserLogin(username, password, result.Value.refreshToken)
                );
        }
        else
        {
            return Result<UserLogin>.Failure(result.ErrorMessage);
        }
    }

    public async Task LogoutAsync()
    {
        var result = await _apiClient.DeleteAsync("api/session/close");
        if (result.IsSuccess)
        {
            await _jwtProvider.ClearAsync();
            IsAuthenticated = false;
        }
        else
        {
            throw new LogoutException(result.ErrorMessage ?? "Something happened");
        }
    }

    public async Task InitializeAsync()
    {
        await _jwtProvider.InitializeAsync();
        IsAuthenticated = _jwtProvider.HasValidRefreshToken;
    }
}
public class LogoutException(string message) : Exception(message);
