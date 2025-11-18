using Govor.Mobile.Application.Models.Results;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Services.Interfaces;

namespace Govor.Mobile.Application.Services.Api;

class UserProfileDonloaderSerivce : IUserProfileDonloaderSerivce
{
    private readonly IApiClient _apiClient;

    public UserProfileDonloaderSerivce(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Result<UserProfile>> GetProfile()
    {
        var result = await _apiClient.GetAsync<UserProfile>("api/profile/dowload");
        
        if(!result.IsSuccess)
            return Result<UserProfile>.Failure(result.ErrorMessage);

        return Result<UserProfile>.Success(result.Value);
    }

    public async Task<Result<UserProfile>> GetProfileByUserId(Guid id)
    {
        var result = await _apiClient.GetAsync<UserProfile>($"api/profile/dowload/{id}");

        if (!result.IsSuccess)
            return Result<UserProfile>.Failure(result.ErrorMessage);

        return Result<UserProfile>.Success(result.Value);
    }
}
