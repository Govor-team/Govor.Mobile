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

    public async Task<UserListItemViewModel> CreateAsync(UserProfileDto profile)
    {
        var avatar = _provider.GetRequiredService<AvatarViewModel>(); 
        await avatar.InitializeAsync(profile.Username, profile.IconId);
        
        var result = await _privateChatApi.GetChatByFriendId(profile.Id);
        
        var vm = new UserListItemViewModel(
            avatar,
            null,
            profile.Id)
        {
            Title = profile.Username,
            IsOnline = profile.IsOnline,
        };
        try
        {
            // предположим здесь:
            var lastMessage = result.IsSuccess ? 
                (await _messages.GetMessagesLocalAsync(result.Value, 1)).FirstOrDefault() : null;
            
            vm.Subtitle = lastMessage?.EncryptedContent;
            
            vm.DateTime = lastMessage != null
                ? _lastSeen.FormatLastSeen(lastMessage.SentAt)
                : String.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FACTORY ERROR] Для {profile.Id}: {ex.Message}");
            // или верни vm без последних сообщений
            // vm.LastMessage = "Ошибка загрузки сообщений";
        }

        return vm;
        
        
    }
}
