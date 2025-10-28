using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces
{
    public interface ITokenStorageService
    {
        Task<bool> SaveRefreshTokenAsync(string token);
        Task<string?> GetRefreshTokenAsync();
        bool DeleteRefreshToken();
    }
}
