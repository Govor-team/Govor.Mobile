using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Govor.Mobile.Services.Implementations
{
    public class JwtProviderService : IJwtProviderService
    {
        private readonly ILogger<JwtProviderService> _logger;
        private readonly ITokenStorageService _storageService;
        private readonly HttpClient _httpClient;

        private string? _accessToken;
        private string? _refreshToken;

        public string? AccessToken => _accessToken;
        public string? RefreshToken => _refreshToken;

        public JwtProviderService(
            ILogger<JwtProviderService> logger,
            ITokenStorageService tokenStorage)
        {
            _logger = logger;
            _storageService = tokenStorage;

            _httpClient = new HttpClient();
        }

        public async Task InitializeAsync()
        {
            _refreshToken = await _storageService.GetRefreshTokenAsync();
        }

        public async Task<Result<RefreshResponse>> RefreshAsync()
        {
            try
            {
                // отправляем refresh запрос на сервер
                var body = JsonSerializer.Serialize(new { refreshToken = _refreshToken });
                var response = await _httpClient.PostAsync("https://localhost:7155/api/auth/token/refresh", // ip
                    new StringContent(body, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return Result<RefreshResponse>.Failure(await response.Content.ReadAsStringAsync());

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<RefreshResponse>(json)!;

                await SetTokensAsync(data.accessToken, data.refreshToken);

                return Result<RefreshResponse>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing JWT tokens.");
                return Result<RefreshResponse>.Failure(ex.Message);
            }
        }

        public async Task SetTokensAsync(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Tokens cannot be null or empty.");

            await _storageService.SaveRefreshTokenAsync(refreshToken);

            _refreshToken = refreshToken;
            _accessToken = accessToken;

            _logger.LogInformation("Tokens successfully saved.");
        }

        public async Task ClearAsync()
        {
            _storageService.DeleteRefreshToken();

            _refreshToken = default;
            _accessToken = default;

            _logger.LogInformation("Tokens cleared.");
            await Task.CompletedTask;
        }
    }
}
