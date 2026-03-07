namespace Govor.Mobile.Services.Interfaces;

public interface INetworkChecker
{
    Task<bool> CheckInternetAsync();
}