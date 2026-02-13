using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.ChatPage;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.PageModels.MainFlow;

[QueryProperty(nameof(ChatIdString), "chatId")]
[QueryProperty(nameof(IsGroup), "isGroup")]
public partial class ChatPageModel : ObservableObject, IInitializableViewModel, IDisposable
{
    private readonly IMessagesListController _controller;
    private readonly IChatHeaderService _headerService; 
    private readonly IFriendsRealtimeService _realtime;
    private readonly IWasOnlineFormater _wasOnlineFormater;
    private readonly IUserProfileService _profileService;
    private readonly IPrivateChatApi _privateChatApi;

    private string _chatIdString;
    public string ChatIdString
    {
        get => _chatIdString;
        set 
        {
            _chatIdString = value;
            if (Guid.TryParse(value, out var guid))
            {
                ChatId = guid; 
            }
        }
    }
    
    [ObservableProperty] private Guid chatId;
    [ObservableProperty] private bool isGroup;
    
    [ObservableProperty] private string messageText;
    [ObservableProperty] private bool canWrite = true;

    public ObservableCollection<MessagesViewModel> Messages => _controller.Messages;
    [ObservableProperty] private ChatHeaderViewModel header;

    public ChatPageModel(
        IMessagesListController controller, 
        IWasOnlineFormater wasOnlineFormater,
        IFriendsRealtimeService realtime, 
        IUserProfileService profileService,
        IPrivateChatApi privateChatApi,
        IChatHeaderService headerService)
    {
        _controller = controller;
        _headerService = headerService;
        _realtime = realtime;
        _wasOnlineFormater = wasOnlineFormater;
        _profileService = profileService;
        _privateChatApi = privateChatApi;

        // Инициализируем команду назад здесь или через RelayCommand
        _GoBackCommand = new AsyncRelayCommand(OnGoBack);
    }

    public bool IsLoaded { get; set; }
    
    private Guid _ChatIdForHeader = Guid.Empty;
    private IAsyncRelayCommand _GoBackCommand { get; }

    public async Task InitAsync()
    {
        if (IsLoaded) return;

        if (!IsGroup)
        {
            var result = await _privateChatApi.GetChatByFriendId(ChatId);
            if (result.IsSuccess)
            {
                _ChatIdForHeader = ChatId;
                ChatId = result.Value;
            }
        }
        else
        {
            _ChatIdForHeader = ChatId;
        }
        
        Header = await _headerService.BuildAsync(_ChatIdForHeader, IsGroup, _GoBackCommand);
        
        var userProfile = await _profileService.GetCurrentProfile();
        
        await _controller.InitializeAsync(ChatId, userProfile.Id, IsGroup);

        if (!IsGroup)
        {
            UnsubscribeRealtimeEvents(); 
            _realtime.OnUserOnline += SetOnline;
            _realtime.OnUserOffline += SetOffline;
            _realtime.OnUserAvatarUpdate += SetUserAvatarAsync;
        }
        
        IsLoaded = true;
    }

    private async Task OnGoBack() => await Shell.Current.GoToAsync("..");

    private void SetOnline(Guid userId) => UpdateOnlineStatus(userId, true);
    private void SetOffline(Guid userId) => UpdateOnlineStatus(userId, false);

    private void UpdateOnlineStatus(Guid userId, bool online)
    {
        if (ChatId != userId || Header == null) return;

        // SignalR работает в фоне, UI обновляем в MainThread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Header.IsOnline = online;
            Header.Subtitle = _wasOnlineFormater.FormatIsOnline(online);
        });
    }

    private async Task SetUserAvatarAsync(Guid userId, Guid avatarId)
    {
        if (ChatId != userId || Header?.Avatar == null) return;
        
        await MainThread.InvokeOnMainThreadAsync(async () => 
        {
            await Header.Avatar.InitializeAsync(Header.Title, avatarId);
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || !CanWrite) return;
        
        var textToSend = MessageText;
        MessageText = string.Empty;

        var result = await _controller.SendAsync(ChatId, textToSend, IsGroup);
        
        if (!result.IsSuccess)
        {
            MessageText = textToSend; 
        }
    }

    // Очистка при закрытии страницы
    public void Dispose()
    {
        UnsubscribeRealtimeEvents();
    }

    private void UnsubscribeRealtimeEvents()
    {
        _realtime.OnUserOnline -= SetOnline;
        _realtime.OnUserOffline -= SetOffline;
        _realtime.OnUserAvatarUpdate -= SetUserAvatarAsync;
    }
}