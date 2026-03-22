using System.Collections.ObjectModel;
using Govor.Mobile.Data;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;

namespace Govor.Mobile.Services.Interfaces.ChatPage;

public interface IMessagesListController
{
    Task InitializeAsync(Guid chatId, Guid currentUserId, bool isGroup);
    Task<List<MessagesGroupModel>> LoadOlderMessagesAsync(Guid chatId, Guid? oldestMessageId = null);
    ObservableRangeCollection<MessagesGroupModel> MessageGroups { get; }
    Task<Result<bool>> SendAsync(Guid chatId, string text);
}