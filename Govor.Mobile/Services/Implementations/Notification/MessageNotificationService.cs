using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Notification;
using Govor.Mobile.Services.Interfaces.Profiles;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace Govor.Mobile.Services.Implementations.Notification;

public class MessageNotificationService : IMessageNotificationService
{
    private readonly IUserProfileService _profileService;
    private readonly IMessageWasSendedFormater _messageWasSendedFormater;
    
    private bool _firsInit = true;
    public MessageNotificationService(
        IUserProfileService profileService,
        IMessageWasSendedFormater messageWasSendedFormater)
    {
        _profileService = profileService;
        _messageWasSendedFormater = messageWasSendedFormater;
    }
    
    public async Task ShowMessageNotification(MessageResponse message)
    {
        var profile = await _profileService.GetProfileAsync(message.SenderId);
        var sender = profile.Username;
        
        var notification = new NotificationRequest
        {
            NotificationId = Random.Shared.Next(1000, 9999),
            Title = sender,
            Description = message.EncryptedContent,
            Subtitle = _messageWasSendedFormater.Format(message.SentAt),
            Android =
            {
                IconSmallName = new AndroidIcon("govor"),
                ChannelId = "messages",
                Priority = AndroidPriority.High,
                AutoCancel = true,
                VisibilityType = AndroidVisibilityType.Public
            },
        };
        
        await LocalNotificationCenter.Current.Show(notification);
    }
}