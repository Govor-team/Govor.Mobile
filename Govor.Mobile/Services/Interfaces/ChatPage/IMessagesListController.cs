using System.Collections.ObjectModel;
using Govor.Mobile.Data;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces.ChatPage;

public interface IMessagesListController
{
    Task InitializeAsync(Guid chatId, Guid currentUserId, bool isGroup);
    Task<List<MessagesViewModel>> LoadOlderMessagesAsync(Guid chatId, Guid? oldestMessageId = null);
    ObservableRangeCollection<MessagesViewModel> Messages { get; }
    Task<Result<bool>> SendAsync(Guid chatId, string text);
}