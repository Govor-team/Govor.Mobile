using System.Collections.ObjectModel;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces;

public interface IMessageStore
{
    Task<ObservableCollection<MessagesViewModel>> GetOrLoadChatAsync(
        Guid chatId,
        Guid currentUserId,
        bool isGroup);
    void AddOrUpdate(MessageResponse message, Guid currentUserId);
    void Remove(Guid messageId, Guid chatId, bool isGroup);
    void ClearChat(Guid chatId, bool isGroup);
}