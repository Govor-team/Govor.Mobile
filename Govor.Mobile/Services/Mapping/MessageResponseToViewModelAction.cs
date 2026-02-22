using System.Collections.ObjectModel;
using AutoMapper;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Mapping;

public class MessageResponseToViewModelAction : IMappingAction<MessageResponse, MessagesViewModel>
{
    private readonly IAvatartVMCreater _avatartCreator;
    public MessageResponseToViewModelAction(IAvatartVMCreater avatartVmCreater) => _avatartCreator = avatartVmCreater;
    
    public async void Process(MessageResponse source, MessagesViewModel destination, ResolutionContext context)
    {
        // Безопасное получение Items
        var items = context.TryGetItems(out _);
        
        if (items && context.Items.TryGetValue("CurrentUserId", out var idObj) && idObj is Guid myId)
        {
            destination.IsIncoming = source.SenderId != myId;
        }
        else
        {
            // Фоллбек, если ID не был передан (например, при тестах)
            destination.IsIncoming = true; 
        }
        
        destination.Time = source.SentAt.ToLocalTime().ToString("HH:mm");

        await MainThread.InvokeOnMainThreadAsync(async () =>
            destination.Avatar = await _avatartCreator.CreateAvatar(source.SenderId));
            
        if (source.MediaAttachments?.Any() == true)
        {
            // Маппим вложения, используя тот же контекст
            // var attachments = context.Mapper.Map<List<MediaAttachmentViewModel>>(source.MediaAttachments);
            // destination.Attachments = new ObservableCollection<MediaAttachmentViewModel>(attachments);
        }
    }
}