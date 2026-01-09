namespace Govor.Mobile.Services.Hubs;

public interface IPresenceHubService : IHubClient
{
    public event Action<Guid>? OnUserOnline;
    public event Action<Guid>? OnUserOffline;
}