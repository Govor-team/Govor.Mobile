using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class FriendsRequestQueryService : IFriendsRequestQueryService
{
    private readonly IApiClient _apiClient;

    public FriendsRequestQueryService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<Result<List<FriendshipDto>>> GetIncomingRequests()
    {
        var result = await _apiClient.GetAsync<List<FriendshipDto>>($"/api/friends/requests",
            authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<FriendshipDto>>.Success(
                result.Value ?? new List<FriendshipDto>()
            );

        }
        else
        {
            return Result<List<FriendshipDto>>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<List<FriendshipDto>>> GetResponses()
    {
        var result = await _apiClient.GetAsync<List<FriendshipDto>>($"/api/friends/responses",
            authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<FriendshipDto>>.Success(
                result.Value ?? new List<FriendshipDto>()
            );
        }
        else
        {
            return Result<List<FriendshipDto>>.Failure(result.ErrorMessage);
        }
    }
}