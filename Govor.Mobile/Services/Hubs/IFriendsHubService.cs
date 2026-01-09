using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Hubs;

public interface IFriendsHubService : IHubClient
{
    public event Action<FriendshipDto>? YourFriendRequestReceived;
    public event Action<FriendshipDto>? FriendRequestReceived;
    
    public event Action<FriendshipDto>? YourFriendRequestAccepted;
    public event Action<FriendshipDto>? FriendRequestAccepted;
    
    public event Action<FriendshipDto>? YourFriendRequestRejected;
    public event Action<FriendshipDto>? FriendRequestRejected;
    
    Task<HubResult<object>> SendRequestAsync(Guid targetUserId);
    Task<HubResult<object>> AcceptRequestAsync(Guid friendshipId);
    Task<HubResult<object>> RejectRequestAsync(Guid friendshipId);
}