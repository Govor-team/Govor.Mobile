using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Interfaces.Notification;

public interface IMessageNotificationService
{
    Task ShowMessageNotification(MessageResponse message);
}