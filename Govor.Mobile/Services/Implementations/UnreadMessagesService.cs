using System.Collections.Concurrent;
using Govor.Mobile.Data;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Repositories;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Utilities;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

public sealed class UnreadMessagesService : IUnreadMessagesService, IDisposable
{
    private readonly IJwtProviderService _session;
    private readonly IMessagesRepository _messages;
    private readonly IChatHub _hub;
    private readonly IApiClient _api;
    private readonly LocalAccountCache _cache;
    private readonly ILogger<UnreadMessagesService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<(Guid ChatId, bool IsGroup), int> _counts = new();
    private readonly ConcurrentDictionary<Guid, byte> _received = new();
    private readonly ConcurrentDictionary<Guid, byte> _read = new();
    private readonly ConcurrentDictionary<(Guid ChatId, bool IsGroup), CoalescingRefresh> _refreshes = new();
    private readonly ConcurrentDictionary<(Guid ChatId, bool IsGroup), int> _pendingDeltas = new();
    public event Action<Guid, bool, int>? UnreadCountChanged;

    public UnreadMessagesService(IMessagesRepository messages, IJwtProviderService session,
        IChatHub hub, IApiClient api, LocalAccountCache cache, ILogger<UnreadMessagesService> logger)
    {
        _messages = messages; _session = session; _hub = hub;
        _api = api; _cache = cache; _logger = logger;
        messages.OnNewMessage += OnNewMessage;
        messages.OnMessageViewed += OnMessageViewed;
        messages.OnMessageDeleted += OnMessageDeleted;
        hub.ChatRead += OnChatRead;
        hub.Reconnected += RefreshTrackedChats;
        session.WasClearTokens += Clear;
    }

    private void Clear()
    {
        foreach (var refresh in _refreshes.Values) refresh.Dispose();
        _refreshes.Clear(); _pendingDeltas.Clear();
        _counts.Clear(); _received.Clear(); _read.Clear();
    }

    private void OnNewMessage(MessageResponse message)
    {
        if (message.Id == Guid.Empty || message.RecipientId == Guid.Empty
            || message.SenderId == _session.CurrentUserId || !_received.TryAdd(message.Id, 0)) return;
        var alreadyRead = _read.ContainsKey(message.Id) || message.MessageViews.Any(v => v.UserId == _session.CurrentUserId);
        QueueRefresh(message.RecipientId, message.RecipientType == RecipientType.Group, alreadyRead ? 0 : 1);
    }

    private void OnMessageViewed(MessageView view)
    {
        if (view.UserId == _session.CurrentUserId && _read.TryAdd(view.MessageId, 0))
            QueueRefresh(view.ChatId, view.RecipientType == RecipientType.Group, -1);
    }

    private void OnChatRead(ChatReadResponse receipt)
    {
        if (receipt.ReaderId == _session.CurrentUserId)
            // Read receipts can arrive out of order. Re-query rather than applying an old count.
            QueueRefresh(receipt.ChatId, receipt.RecipientType == RecipientType.Group);
    }

    private void OnMessageDeleted(Guid _) => RefreshTrackedChats();
    private void RefreshTrackedChats()
    {
        foreach (var chat in _counts.Keys)
            QueueRefresh(chat.ChatId, chat.IsGroup);
    }

    public Task<int> InitializeAsync(Guid chatId, bool isGroup = false) => RefreshAsync(chatId, isGroup);

    public async Task<int> GetCachedCountAsync(Guid chatId, bool isGroup = false)
    {
        var account = _session.CurrentUserId;
        if (chatId == Guid.Empty || account == null) return 0;
        var key = (chatId, isGroup);
        if (_counts.TryGetValue(key, out var known)) return known;
        var saved = await _cache.ReadAsync<UnreadCountResponse>(CacheKey(chatId, isGroup));
        if (_session.CurrentUserId != account) return 0;
        return _counts.GetOrAdd(key, Math.Max(0, saved?.UnreadCount ?? 0));
    }

    private static string CacheKey(Guid chatId, bool isGroup) => $"unread-{(isGroup ? "group" : "private")}-{chatId:N}";

    public async Task<bool> MarkAsReadAsync(Guid chatId, Guid messageId, bool isGroup = false)
    {
        if (chatId == Guid.Empty || messageId == Guid.Empty || !_read.TryAdd(messageId, 0)) return false;
        await RefreshAsync(chatId, isGroup, -1);
        return true;
    }

    private void QueueRefresh(Guid chatId, bool isGroup, int offlineDelta = 0)
    {
        if (chatId == Guid.Empty || _session.CurrentUserId == null) return;
        var key = (chatId, isGroup);
        _pendingDeltas.AddOrUpdate(key, offlineDelta, (_, current) => current + offlineDelta);
        var refresh = _refreshes.GetOrAdd(key, _ => new CoalescingRefresh(async _ =>
        {
            _pendingDeltas.TryRemove(key, out var delta);
            await RefreshAsync(chatId, isGroup, delta);
        }, ex => _logger.LogWarning(ex, "Unable to refresh unread count for {ChatId}", chatId)));
        refresh.Request();
    }

    private async Task<int> RefreshAsync(Guid chatId, bool isGroup, int offlineDelta = 0)
    {
        var account = _session.CurrentUserId;
        if (chatId == Guid.Empty || account == null) return 0;
        await _gate.WaitAsync();
        try
        {
            if (_session.CurrentUserId != account) return 0;
            var key = (chatId, isGroup);
            var cacheKey = CacheKey(chatId, isGroup);
            var previous = await GetCachedCountAsync(chatId, isGroup);
            if (_session.CurrentUserId != account) return 0;
            var result = await _api.GetAsync<UnreadCountResponse>(
                $"api/chats/{chatId}/unread-count?recipientType={(isGroup ? 1 : 0)}");
            if (_session.CurrentUserId != account) return 0;
            // The API includes all history, including messages not loaded on this device.
            var count = Math.Max(0, result.IsSuccess && result.Value != null
                ? result.Value.UnreadCount : previous + offlineDelta);
            _counts[key] = count;
            await _cache.WriteAsync(cacheKey, new UnreadCountResponse { UnreadCount = count });
            if (_session.CurrentUserId == account) UnreadCountChanged?.Invoke(chatId, isGroup, count);
            return count;
        }
        finally { _gate.Release(); }
    }

    public void Dispose()
    {
        _messages.OnNewMessage -= OnNewMessage;
        _messages.OnMessageViewed -= OnMessageViewed;
        _messages.OnMessageDeleted -= OnMessageDeleted;
        _hub.ChatRead -= OnChatRead;
        _hub.Reconnected -= RefreshTrackedChats;
        _session.WasClearTokens -= Clear;
        Clear();
    }
}
