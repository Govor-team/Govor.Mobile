using Govor.Mobile.Application.Services.Interfaces.JwtServices;
using Govor.Mobile.Application.Services.Interfaces.Wrappers;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Application.Services.Implementations.JwtServices
{
    public class TokenStorageService : ITokenStorageService
    {
        private const string RefreshTokenKey = "RefreshToken";
        private readonly ILogger<TokenStorageService> _logger;
        private readonly ISecureStorage _secureStorage;

        public TokenStorageService(ILogger<TokenStorageService> logger, ISecureStorage secureStorage)
        {
            _logger = logger;
            _secureStorage = secureStorage;
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
                await _secureStorage.SetAsync(RefreshTokenKey, token);
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
                var token = await _secureStorage.GetAsync(RefreshTokenKey);

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
                _secureStorage.Remove(RefreshTokenKey);
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
