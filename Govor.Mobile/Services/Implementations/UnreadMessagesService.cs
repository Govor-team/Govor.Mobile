using System.Collections.Concurrent;
using Govor.Mobile.Data;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.Services.Interfaces.Repositories;

namespace Govor.Mobile.Services.Implementations;

public sealed class UnreadMessagesService : IUnreadMessagesService, IDisposable
{
    private const int DefaultUnreadCount = 10;
    private readonly ConcurrentDictionary<(Guid ChatId, bool IsGroup), int> _counts = new();
    private readonly ConcurrentDictionary<(Guid ChatId, bool IsGroup), ConcurrentDictionary<Guid, byte>> _readMessages = new();
    private readonly IMessagesRepository _messagesRepository;
    private readonly IUserProfileService _userProfileService;
    public event Action<Guid, bool, int>? UnreadCountChanged;

    public UnreadMessagesService(
        IMessagesRepository messagesRepository,
        IUserProfileService userProfileService)
    {
        _messagesRepository = messagesRepository;
        _userProfileService = userProfileService;

        _messagesRepository.OnMessageViewed += OnMessageViewed;
        _messagesRepository.OnNewMessage += OnNewMessage;
    }

    private void OnNewMessage(MessageResponse response)
    {
        if (response.RecipientId == Guid.Empty || response.Id == Guid.Empty)
            return;

        if(response.SenderId == _userProfileService.GetCurrentProfileAsync().Result.Id)
            return;

        var key = (response.RecipientId, response.RecipientType == RecipientType.Group);

        var count = _counts.AddOrUpdate(key, DefaultUnreadCount, (_, current) => current + 1);

        UnreadCountChanged?.Invoke(
            response.RecipientId,
            response.RecipientType == RecipientType.Group,
            count >= 0 ? count : 0);
    }

    private void OnMessageViewed(MessageView view)
    {
        MarkAsReadAsync(view.ChatId, view.MessageId, view.RecipientType == RecipientType.Group).ContinueWith(t =>
        {
            if (t.IsFaulted)
                Console.WriteLine($"[UnreadMessagesService] Error marking message as read: {t.Exception?.GetBaseException().Message}");
        });
    }

    public Task<int> InitializeAsync(Guid chatId, bool isGroup = false)
    {
        if (chatId == Guid.Empty)
            return Task.FromResult(0);

        var key = (chatId, isGroup);
        _counts.TryAdd(key, DefaultUnreadCount);

        return Task.FromResult(_counts[key]);
    }

    public Task<bool> MarkAsReadAsync(Guid chatId, Guid messageId, bool isGroup = false)
    {
        if (chatId == Guid.Empty || messageId == Guid.Empty)
            return Task.FromResult(false);

        var key = (chatId, isGroup);
        var readMessages = _readMessages.GetOrAdd(key, _ => new ConcurrentDictionary<Guid, byte>());
        if (!readMessages.TryAdd(messageId, 0))
            return Task.FromResult(false);

        var count = _counts.AddOrUpdate(key, DefaultUnreadCount - 1, (_, current) => Math.Max(0, current - 1));
        UnreadCountChanged?.Invoke(chatId, isGroup, count);
        return Task.FromResult(true);
    }

    public void Dispose()
    {
        _messagesRepository.OnMessageViewed -= OnMessageViewed;
        _messagesRepository.OnNewMessage -= OnNewMessage;
    }
}
