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
        if (string.IsNullOrEmpty(token) || string.IsNullOrWhiteSpace(token))
            return Result<bool>.Failure("Push token is empty or null");

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
}