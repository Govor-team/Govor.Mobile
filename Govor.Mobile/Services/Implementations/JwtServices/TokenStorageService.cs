using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Govor.Mobile.Services.Implementations.JwtServices
{
    public class TokenStorageService : ITokenStorageService
    {
        private const string RefreshTokenKey = "RefreshToken";
        private const string SessionKey = "Govor.Session.v1";
        private sealed record StoredSession(string AccessToken, string RefreshToken);

        public async Task<bool> SaveTokensAsync(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                return false;
            try
            {
                // One encrypted entry prevents partially saved token pairs during rotation.
                await SecureStorage.SetAsync(SessionKey,
                    JsonSerializer.Serialize(new StoredSession(accessToken, refreshToken)));
                SecureStorage.Remove(RefreshTokenKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to persist authentication session.");
                return false;
            }
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var session = await ReadSessionAsync();
            return session?.AccessToken;
        }

        private async Task<StoredSession?> ReadSessionAsync()
        {
            try
            {
                var json = await SecureStorage.GetAsync(SessionKey);
                return json is null ? null : JsonSerializer.Deserialize<StoredSession>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read authentication session.");
                SecureStorage.Remove(SessionKey);
                return null;
            }
        }
        private readonly ILogger<TokenStorageService> _logger;

        public TokenStorageService(ILogger<TokenStorageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Сохраняет refresh токен в защищённое хранилище.
        /// </summary>
        public async Task<bool> SaveRefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Attempted to save an empty refresh token.");
                return false;
            }

            try
            {
                await SecureStorage.SetAsync(RefreshTokenKey, token);
                _logger.LogInformation("Refresh token successfully saved to SecureStorage.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving refresh token to SecureStorage.");
                return false;
            }
        }

        /// <summary>
        /// Извлекает refresh токен из защищённого хранилища.
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                var session = await ReadSessionAsync();
                var token = session?.RefreshToken ?? await SecureStorage.GetAsync(RefreshTokenKey);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("No refresh token found in SecureStorage.");
                    return null;
                }

                _logger.LogDebug("Refresh token successfully retrieved from SecureStorage.");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving refresh token from SecureStorage.");
                return null;
            }
        }

        /// <summary>
        /// Удаляет refresh токен из защищённого хранилища.
        /// </summary>
        public bool DeleteRefreshToken()
        {
            try
            {
                SecureStorage.Remove(RefreshTokenKey);
                SecureStorage.Remove(SessionKey);
                _logger.LogInformation("Refresh token successfully deleted from SecureStorage.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting refresh token from SecureStorage.");
                return false;
            }
        }
    }
}
