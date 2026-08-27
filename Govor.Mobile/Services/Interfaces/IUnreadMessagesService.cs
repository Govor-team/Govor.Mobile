namespace Govor.Mobile.Services.Interfaces;

public interface IUnreadMessagesService
{
    event Action<Guid, bool, int>? UnreadCountChanged;

    Task<int> InitializeAsync(Guid chatId, bool isGroup = false);
    Task<bool> MarkAsReadAsync(Guid chatId, Guid messageId, bool isGroup = false);
}
