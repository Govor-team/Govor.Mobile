using AutoMapper;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Mapping;

public sealed class MessageResponseToViewModelAction(
    IAvatartVMCreater avatarCreator, ILogger<MessageResponseToViewModelAction> logger)
    : IMappingAction<MessageResponse, MessagesViewModel>
{
    public void Process(MessageResponse source, MessagesViewModel destination, ResolutionContext context)
    {
        context.TryGetItems(out var items);
        destination.IsOwnMessage = items != null && items.TryGetValue("CurrentUserId", out var id)
            && id is Guid currentId && source.SenderId == currentId;
        destination.IsGroupChat = items != null && items.TryGetValue("IsGroup", out var group) && group is true;
        destination.IsChannel = items != null && items.TryGetValue("IsChannel", out var channel) && channel is true;
        destination.IsIncoming = Govor.Mobile.Models.Groups.MessagePresentation.Resolve(
            destination.IsGroupChat, destination.IsChannel, destination.IsOwnMessage).Incoming;
        destination.Time = source.SentAt.ToLocalTime().ToString("HH:mm");
        // AutoMapper callbacks are synchronous. Observe asynchronous avatar failures explicitly.
        if (!destination.IsChannel) _ = LoadAvatarAsync(source.SenderId, destination);
    }

    private async Task LoadAvatarAsync(Guid senderId, MessagesViewModel destination)
    {
        try
        {
            var avatar = await avatarCreator.CreateAvatar(senderId);
            await MainThread.InvokeOnMainThreadAsync(() => destination.Avatar = avatar);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Unable to load message avatar."); }
    }
}
