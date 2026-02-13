using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class FriendsRealtimeService : IFriendsRealtimeService
{
    private readonly IPresenceHubService _presence;
    private readonly IFriendsHubService _friends;
    private readonly IProfileHubService _profileHub;

    public event Action<Guid> OnUserOnline;
    public event Action<Guid> OnUserOffline;
    public event Func<Guid, string, Task> OnUserDescriptionUpdate;
    public event Func<Guid, Guid, Task> OnUserAvatarUpdate;
    public event Func<Guid, Task> OnFriendAdded;

    public FriendsRealtimeService(
        IProfileHubService profileHub,
        IPresenceHubService presence,
        IFriendsHubService friends)
    {
        _presence = presence;
        _friends = friends;
        _profileHub = profileHub;
        
        _presence.OnUserOnline += id => OnUserOnline?.Invoke(id);
        _presence.OnUserOffline += id => OnUserOffline?.Invoke(id);

        _friends.FriendRequestAccepted += dto => OnFriendAdded?.Invoke(dto.RequesterId);
        _friends.YourFriendRequestAccepted += dto => OnFriendAdded?.Invoke(dto.AddresseeId);
        
        _profileHub.OnDescriptionUpdated += (id, payload) =>  OnUserDescriptionUpdate?.Invoke(payload.UserId,
            payload.Description ?? String.Empty);
        
        _profileHub.OnAvatarUpdated += (guid, payload) => OnUserAvatarUpdate?.Invoke(payload.UserId, payload.IconId);
    }

    public void Dispose()
    {
        // unsubscribe if needed
    }
}
