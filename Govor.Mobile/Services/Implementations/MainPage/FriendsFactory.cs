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
    private readonly IPrivateChatApi _privateChatApi;

    public FriendsFactory(
        IServiceProvider provider,
        IMessagesRepository messages,
        IPrivateChatApi privateChatApi,
        IWasOnlineFormater lastSeen)
    {
        _provider = provider;
        _messages = messages;
        _lastSeen = lastSeen;
        _privateChatApi = privateChatApi;
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

        var vm = new UserListItemViewModel(avatar, null, profile.Id)
        {
            Title = profile.Username,
            IsOnline = profile.IsOnline
        };

        try
        {
            // Получаем последние сообщения для приватного чата
            if (privateChatId != Guid.Empty)
            {
                _ = LoadLastMessageAsync(vm, privateChatId); // фоновой вызов
            }
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
            vm.Subtitle = lastMessage?.EncryptedContent;
            vm.DateTime = lastMessage != null ? _lastSeen.FormatLastSeen(lastMessage.SentAt) : string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FACTORY ERROR] Last message load failed: {ex.Message}");
        }
    }
}