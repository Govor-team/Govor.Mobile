using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Interfaces.JwtServices;

public interface IJwtProviderService
{
    bool HasRefreshToken { get; }
    public event Action WasClearTokens;
    Task<string> GetAccessTokenAsync();

    Task InitializeAsync();
    
    Task InitializeWithTokensAsync(string accessToken, string refreshToken);
    
    Task ClearAsync();
}
