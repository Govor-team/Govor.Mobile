using System.Collections.ObjectModel;
using Govor.Mobile.Data;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces.ChatPage;

public interface IMessagesListController
{
    Task InitializeAsync(Guid chatId, Guid currentUserId, bool isGroup);
    ObservableCollection<MessagesViewModel> Messages { get; }
    Task<Result<bool>> SendAsync(Guid chatId, string text, bool isGroup);
}