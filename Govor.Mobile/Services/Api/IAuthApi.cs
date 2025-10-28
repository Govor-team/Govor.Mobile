using Govor.Mobile.Models.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Api
{
    internal interface IAuthApi
    {
        Task<Result<AuthResponse>> LoginAsync(string username, string password, string deviceInfo);
        Task<Result<AuthResponse>> RegisterAsync(string  username, string password, string inviteLink, string deviceInfo);
        Task<Result<RefreshResponse>> RefreshTokenAsync(string refreshToken);
        Task<Result<AuthResponse>> LogoutAsync();
    }
}
