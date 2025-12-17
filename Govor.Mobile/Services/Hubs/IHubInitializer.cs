namespace Govor.Mobile.Services.Hubs;

public interface IHubInitializer
{
    Task ConnectAllAsync();
    Task DisconnectAllAsync();
}