using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Interfaces.JwtServices;

public interface IJwtProviderService
{
    bool HasRefreshToken { get; }
    Guid? CurrentUserId { get; }
    public event Action WasClearTokens;
    Task<string> GetAccessTokenAsync();
    Task<string> RefreshAccessTokenAsync(string? rejectedToken);

    Task InitializeAsync();
    
    Task InitializeWithTokensAsync(string accessToken, string refreshToken);
    
    Task ClearAsync();
}
