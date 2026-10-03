using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces.JwtServices;

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync();
    Task<bool> SaveTokensAsync(string accessToken, string refreshToken);
    Task<bool> SaveRefreshTokenAsync(string token);
    Task<string?> GetRefreshTokenAsync();
    bool DeleteRefreshToken();
}
