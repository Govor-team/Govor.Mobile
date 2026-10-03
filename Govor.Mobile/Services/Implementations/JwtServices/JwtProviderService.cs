using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations.JwtServices;

public sealed class JwtProviderService : IJwtProviderService
{
    private readonly ILogger<JwtProviderService> _logger;
    private readonly ITokenStorageService _storage;
    private readonly IServerIpProvider _server;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt;
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromSeconds(30);
    public bool HasRefreshToken => !string.IsNullOrWhiteSpace(_refreshToken);
    public Guid? CurrentUserId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_accessToken)) return null;
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_accessToken);
            return Guid.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value, out var id) ? id : null;
        }
    }
    public event Action? WasClearTokens;

    public JwtProviderService(ILogger<JwtProviderService> logger,
        IServerIpProvider server, ITokenStorageService storage, IHttpClientFactory factory)
    {
        _logger = logger;
        _server = server;
        _storage = storage;
        _httpClient = factory.CreateClient("TokenRefresh");
    }

    public async Task InitializeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _refreshToken = await _storage.GetRefreshTokenAsync();
            _accessToken = await _storage.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(_accessToken))
            {
                try { _expiresAt = ExtractExpiration(_accessToken); }
                catch { _accessToken = null; }
            }
        }
        finally { _gate.Release(); }
    }

    public Task<string> GetAccessTokenAsync() => GetTokenAsync(false, null);
    public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetTokenAsync(true, rejectedToken);

    private async Task<string> GetTokenAsync(bool forceRefresh, string? rejectedToken)
    {
        await _gate.WaitAsync();
        var sessionInvalidated = false;
        try
        {
            // A concurrent request may already have rotated the rejected access token.
            if (HasValidAccessToken() && (!forceRefresh || _accessToken != rejectedToken))
                return _accessToken!;
            if (!HasRefreshToken)
                throw new UnauthorizedAccessException("Authentication session is missing.");

            using var response = await _httpClient.PostAsJsonAsync(
                $"{_server.IP.TrimEnd('/')}/api/auth/token/refresh", new { refreshToken = _refreshToken });
            if (!response.IsSuccessStatusCode)
            {
                var invalid = response.StatusCode == HttpStatusCode.Unauthorized;
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        invalid = error.RootElement.TryGetProperty("errorCode", out var code) &&
                            code.GetString() is "Auth.InvalidToken" or "Auth.EmptyToken";
                    }
                    catch (JsonException) { }
                }
                if (invalid)
                {
                    ClearInternal();
                    sessionInvalidated = true;
                    throw new UnauthorizedAccessException("Authentication session has expired. Please sign in again.");
                }
                // Network failures and server errors preserve the persisted session.
                throw new HttpRequestException("Unable to refresh authentication session.", null, response.StatusCode);
            }
            var tokens = await response.Content.ReadFromJsonAsync<RefreshResponse>()
                ?? throw new InvalidDataException("Empty token refresh response.");
            await SetTokensInternalAsync(tokens.accessToken, tokens.refreshToken);
            return _accessToken!;
        }
        finally
        {
            _gate.Release();
            if (sessionInvalidated) WasClearTokens?.Invoke();
        }
    }

    public async Task InitializeWithTokensAsync(string accessToken, string refreshToken)
    {
        await _gate.WaitAsync();
        try { await SetTokensInternalAsync(accessToken, refreshToken); }
        finally { _gate.Release(); }
    }

    private async Task SetTokensInternalAsync(string accessToken, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidDataException("Refresh token is empty.");
        var expiration = ExtractExpiration(accessToken);
        if (!await _storage.SaveTokensAsync(accessToken, refreshToken))
            throw new IOException("Unable to save authentication session in secure storage.");
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _expiresAt = expiration;
        _logger.LogInformation("Authentication session persisted.");
    }

    public async Task ClearAsync()
    {
        await _gate.WaitAsync();
        try { ClearInternal(); }
        finally { _gate.Release(); }
        WasClearTokens?.Invoke();
    }

    private void ClearInternal()
    {
        if (!_storage.DeleteRefreshToken())
            throw new IOException("Unable to remove authentication session from secure storage.");
        _accessToken = null;
        _refreshToken = null;
        _expiresAt = default;
    }

    private bool HasValidAccessToken() => !string.IsNullOrWhiteSpace(_accessToken) &&
        DateTimeOffset.UtcNow < _expiresAt - ExpirationBuffer;

    private static DateTimeOffset ExtractExpiration(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Payload.Expiration is long exp
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : throw new InvalidDataException("Access token has no expiration.");
    }
}
