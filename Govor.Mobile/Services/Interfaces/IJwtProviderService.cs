using Govor.Mobile.Models.Responses;
using Govor.Mobile.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces
{
    public interface IJwtProviderService
    {
        public bool HasValidRefreshToken => !string.IsNullOrEmpty(RefreshToken);
        public string? AccessToken { get; }
        public string? RefreshToken { get; }

        Task<Result<RefreshResponse>> RefreshAsync();
        Task SetTokensAsync(string accessToken, string refreshToken);
        Task InitializeAsync();
        Task ClearAsync();
    }
}
