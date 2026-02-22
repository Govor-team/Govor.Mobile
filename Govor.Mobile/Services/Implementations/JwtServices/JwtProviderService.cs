using System.IdentityModel.Tokens.Jwt;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations.JwtServices;

public sealed class JwtProviderService : IJwtProviderService
{
    public bool HasRefreshToken => !string.IsNullOrWhiteSpace(_refreshToken);
    public event Action WasClearTokens;

    private readonly ILogger<JwtProviderService> _logger;
    private readonly ITokenStorageService _storageService;
    private readonly IServerIpProvider _ipProvider;
    private readonly HttpClient _httpClient;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _accessTokenExpiration;

    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromSeconds(30);

    public JwtProviderService(
        ILogger<JwtProviderService> logger,
        IServerIpProvider serverIpProvider,
        ITokenStorageService tokenStorage)
    {
        _logger = logger;
        _storageService = tokenStorage;
        _ipProvider = serverIpProvider;
        _httpClient = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        _refreshToken = await _storageService.GetRefreshTokenAsync();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (HasValidAccessToken())
            return _accessToken!;

        await _refreshLock.WaitAsync();
        try
        {
            // повторная проверка после ожидания
            if (HasValidAccessToken())
                return _accessToken!;

            if (string.IsNullOrWhiteSpace(_refreshToken))
                throw new InvalidOperationException("Refresh token is missing.");

            var result = await RefreshInternalAsync();
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage);

            return _accessToken!;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task ClearAsync()
    {
        _storageService.DeleteRefreshToken();

        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiration = default;
        
        WasClearTokens?.Invoke();
        _logger.LogInformation("JWT tokens cleared.");
    }
    
    private bool HasValidAccessToken()
    {
        return !string.IsNullOrEmpty(_accessToken) &&
               DateTimeOffset.UtcNow < _accessTokenExpiration - ExpirationBuffer;
    }

    private async Task<Result<RefreshResponse>> RefreshInternalAsync()
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                refreshToken = _refreshToken
            });

            var response = await _httpClient.PostAsync(
                $"{_ipProvider.IP}/api/auth/token/refresh",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                await ClearAsync();
                
                return Result<RefreshResponse>.Failure(
                    await response.Content.ReadAsStringAsync());
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<RefreshResponse>(json)!;

            await SetTokensInternalAsync(data.accessToken, data.refreshToken);

            return Result<RefreshResponse>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT refresh failed.");
            return Result<RefreshResponse>.Failure(ex.Message);
        }
    }

    private async Task SetTokensInternalAsync(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;

        _accessTokenExpiration = ExtractExpiration(accessToken);

        await _storageService.SaveRefreshTokenAsync(refreshToken);
        _logger.LogInformation("JWT tokens updated successfully.");
    }

    private static DateTimeOffset ExtractExpiration(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        if (jwt.Payload.Exp is int exp)
            return DateTimeOffset.FromUnixTimeSeconds(exp);

        return DateTimeOffset.UtcNow.AddMinutes(5);
    }
    
    public async Task InitializeWithTokensAsync(string accessToken, string refreshToken)
    {
        await _refreshLock.WaitAsync();
        try
        {
            await SetTokensInternalAsync(accessToken, refreshToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}