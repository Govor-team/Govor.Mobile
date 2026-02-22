namespace Govor.Mobile.Services;

public interface IConnectivityChanged
{
    Task OnInternetConnectedAsync();
    Task OnInternetDisconnectedAsync();
}