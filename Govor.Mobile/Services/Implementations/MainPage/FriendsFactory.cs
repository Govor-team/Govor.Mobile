using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Repositories;

namespace Govor.Mobile.Services.Implementations.MainPage;

public class FriendsFactory : IFriendsFactory
{
    private readonly IServiceProvider _provider;
    private readonly IMessagesRepository _messages;
    private readonly IWasOnlineFormater _lastSeen;
    private readonly IUnreadMessagesService _unreadMessages;

    public FriendsFactory(
        IServiceProvider provider,
        IMessagesRepository messages,
        IWasOnlineFormater lastSeen,
        IUnreadMessagesService unreadMessages)
    {
        _provider = provider;
        _messages = messages;
        _lastSeen = lastSeen;
        _unreadMessages = unreadMessages;
    }

    public async Task<UserListItemViewModel> CreateAsync(UserProfileDto profile, Guid privateChatId)
    {
        // Инициализация аватара
        var avatar = _provider.GetRequiredService<AvatarViewModel>();
        
        _ = avatar.InitializeAsync(profile.Username, profile.IconId)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"[Avatar ERROR] {profile.Id}: {t.Exception?.GetBaseException().Message}");
            });

      

        var unreadCount = await _unreadMessages.InitializeAsync(privateChatId);
        var vm = new UserListItemViewModel(avatar, null, unreadCount, userId: profile.Id, chatId: privateChatId)
        {
            Title = profile.Username,
            IsOnline = profile.IsOnline
        };

        try
        {
            if (privateChatId != Guid.Empty)
                await LoadLastMessageAsync(vm, privateChatId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FACTORY ERROR] Для {profile.Id}: {ex.Message}");
        }

        return vm;
    }
    
    private async Task LoadLastMessageAsync(UserListItemViewModel vm, Guid privateChatId)
    {
        try
        {
            var lastMessage = (await _messages.GetMessagesLocalAsync(privateChatId, 1)).FirstOrDefault();
            if (lastMessage != null)
                UpdatePreview(vm, lastMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FACTORY ERROR] Last message load failed: {ex.Message}");
        }
    }

    public void UpdatePreview(UserListItemViewModel friend, MessageResponse message)
    {
        if (friend.ChatId != message.RecipientId ||
            (friend.LastMessageSentAt.HasValue && friend.LastMessageSentAt.Value > message.SentAt))
            return;

        friend.Subtitle = message.EncryptedContent;
        friend.DateTime = _lastSeen.FormatLastSeen(message.SentAt);
        friend.SetLastMessageSentAt(message.SentAt);
    }
}