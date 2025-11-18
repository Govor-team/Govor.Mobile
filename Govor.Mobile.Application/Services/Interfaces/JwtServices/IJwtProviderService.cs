using Govor.Mobile.Application.Models.Responses;

namespace Govor.Mobile.Application.Services.Interfaces.JwtServices;

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
