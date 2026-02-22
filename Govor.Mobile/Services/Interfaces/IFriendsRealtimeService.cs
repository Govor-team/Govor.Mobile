namespace Govor.Mobile.Services.Interfaces;

public interface IFriendsRealtimeService : IDisposable
{
    event Action<Guid> OnUserOnline;
    event Action<Guid> OnUserOffline;
    event Func<Guid, string, Task> OnUserDescriptionUpdate;
    event Func<Guid, Guid, Task> OnUserAvatarUpdate;
    event Func<Guid, Task> OnFriendAdded;
    event Func<Guid, Task> OnFriendRemoved;
}