namespace Govor.Mobile.Services.Hubs;

public interface IHubClient
{
    Task ConnectAsync();
    Task DisconnectAsync();
}