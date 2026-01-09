using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class FriendshipApiService : IFriendshipApiService
{
    private readonly IApiClient _apiClient;

    public FriendshipApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<Result<List<UserDto>>> Search(string query)
    {
        var result = await _apiClient.GetAsync<List<UserDto>>($"/api/friends/search?query={query}",
            authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<UserDto>>.Success(
                result.Value ?? new List<UserDto>()
            );

        }
        else
        {
            return Result<List<UserDto>>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<List<UserDto>>> GetFriends()
    {
        var result = await _apiClient.GetAsync<List<UserDto>>($"api/friends",
            authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<UserDto>>.Success(
                result.Value ?? new List<UserDto>()
            );
        }
        else
        {
            return Result<List<UserDto>>.Failure(result.ErrorMessage);
        }
    }
}