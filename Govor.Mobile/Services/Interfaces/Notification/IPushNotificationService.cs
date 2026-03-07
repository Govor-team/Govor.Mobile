namespace Govor.Mobile.Services.Interfaces.Notification;

public interface IPushNotificationService
{
    Task InitializeAsync();
    Task UnregisterAsync();
}