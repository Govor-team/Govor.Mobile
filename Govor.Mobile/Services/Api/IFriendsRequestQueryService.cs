using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api;

public interface IFriendsRequestQueryService
{
    Task<Result<List<FriendshipDto>>> GetIncomingRequests();
    Task<Result<List<FriendshipDto>>> GetResponses();
}