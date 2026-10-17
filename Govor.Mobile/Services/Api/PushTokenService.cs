using Govor.Mobile.Models.Requests;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class PushTokenService : IPushTokenService
{
    private readonly IApiClient _apiClient;
    
    public PushTokenService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<Result<bool>> PushToken(string token, string platform)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
            return Result<bool>.Failure("Push token must contain 1–512 characters");
        if (string.IsNullOrWhiteSpace(platform) || platform.Length > 50)
            return Result<bool>.Failure("Push platform must contain 1–50 characters");

        var req = new RegisterPushTokenRequest()
        {
            Token = token,
            Platform = platform
        };
        
        var path = $"/api/pushes/token/register";
        var result = await _apiClient.PostAsync<bool>(path, req, authenticated: true);

        if (result.IsSuccess)
        {
            return Result<bool>.Success(true);
        }
        else
        {
            return Result<bool>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<bool>> DeleteTokenAsync()
    {
        var result = await _apiClient.DeleteAsync("/api/pushes/token", authenticated: true);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.ErrorMessage);
    }
}
