using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
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
    
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasMoreMessages = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectionMode))]
    [NotifyPropertyChangedFor(nameof(SelectedMessageCount))]
    [NotifyPropertyChangedFor(nameof(CanEditMessage))]
    private ObservableCollection<MessagesViewModel> selectedMessages = new();

    [ObservableProperty] private bool isEditingMessage;
    [ObservableProperty] private MessagesViewModel? editingMessage;

    public bool IsSelectionMode => SelectedMessages.Count > 0;
    public int SelectedMessageCount => SelectedMessages.Count;
    public bool CanCopyMessages => SelectedMessages.Count > 0;
    public bool CanForwardMessages => SelectedMessages.Count > 0;
    public bool CanDeleteMessages => SelectedMessages.Count > 0;
    public bool CanEditMessage => SelectedMessages.Count == 1 && !SelectedMessages[0].IsIncoming;

    public ObservableRangeCollection<MessagesGroupModel> MessageGroups => _controller.MessageGroups;
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
        if (IsLoaded)
            return;

        var profileTask = _profileService.GetCurrentProfile();

        Task<Result<Guid>?> chatTask = null;

        if (!IsGroup)
        {
            chatTask = _privateChatApi.GetChatByFriendId(ChatId);
        }
        else
        {
            _ChatIdForHeader = ChatId;
        }

        if (chatTask != null)
        {
            var result = await chatTask;

            if (result.IsSuccess)
            {
                _ChatIdForHeader = ChatId;
                ChatId = result.Value;
            }
        }

        // Теперь header можно строить параллельно с profile
        var headerTask = _headerService.BuildAsync(
            _ChatIdForHeader,
            IsGroup,
            _GoBackCommand);

        await Task.WhenAll(
            headerTask,
            profileTask);

        var profile = await profileTask;

        // Минимально необходимое для отображения страницы
        Header = await headerTask;

        if (!IsGroup)
        {
            UnsubscribeRealtimeEvents();

            _realtime.OnUserOnline += SetOnline;
            _realtime.OnUserOffline += SetOffline;
            _realtime.OnUserAvatarUpdate += SetUserAvatarAsync;
        }

        IsLoaded = true;

        // Не мешаем открытию UI
        _ = InitializeControllerAsync(
            ChatId,
            profile.Id,
            IsGroup);
    }
    private async Task InitializeControllerAsync(
                                    Guid chatId,
                                    Guid userId,
                                    bool isGroup)
    {
        try
        {
            await _controller.InitializeAsync(
                chatId,
                userId,
                isGroup);
        }
        catch (Exception ex)
        {
            // log
        }
    }
    private async Task OnGoBack() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private void ToggleMessageSelection(MessagesViewModel message)
    {
        if (SelectedMessages.Contains(message))
            SelectedMessages.Remove(message);
        else
            SelectedMessages.Add(message);

        message.IsSelected = SelectedMessages.Contains(message);
        UpdateSelectionModeVisibility();
        OnPropertyChanged(nameof(IsSelectionMode));
        OnPropertyChanged(nameof(SelectedMessageCount));
        OnPropertyChanged(nameof(CanCopyMessages));
        OnPropertyChanged(nameof(CanForwardMessages));
        OnPropertyChanged(nameof(CanDeleteMessages));
        EditMessageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ExitMessageSelection()
    {
        ClearSelection();
        CancelMessageEditing();
    }

    [RelayCommand(CanExecute = nameof(CanEditMessageCommand))]
    private void EditMessage()
    {
        var message = SelectedMessages.SingleOrDefault();
        if (message == null)
            return;

        EditingMessage = message;
        IsEditingMessage = true;
        MessageText = message.Text;
        ClearSelection();
    }

    private bool CanEditMessageCommand() => CanEditMessage;

    [RelayCommand]
    private async Task CopyMessages()
    {
        var text = string.Join(Environment.NewLine, SelectedMessages.Select(message => message.Text));
        if (!string.IsNullOrWhiteSpace(text))
            await Clipboard.Default.SetTextAsync(text);
    }

    [RelayCommand]
    private void ForwardMessages()
    {
        ClearSelection();
    }

    [RelayCommand]
    private void DeleteMessages()
    {
        ClearSelection();
    }

    [RelayCommand]
    private void CancelMessageEditing()
    {
        IsEditingMessage = false;
        EditingMessage = null;
        MessageText = string.Empty;
    }

    private void ClearSelection()
    {
        foreach (var message in SelectedMessages)
            message.IsSelected = false;

        SelectedMessages.Clear();
        UpdateSelectionModeVisibility();
        OnPropertyChanged(nameof(IsSelectionMode));
        OnPropertyChanged(nameof(SelectedMessageCount));
        OnPropertyChanged(nameof(CanCopyMessages));
        OnPropertyChanged(nameof(CanForwardMessages));
        OnPropertyChanged(nameof(CanDeleteMessages));
        EditMessageCommand.NotifyCanExecuteChanged();
    }

    private void UpdateSelectionModeVisibility()
    {
        var isVisible = SelectedMessages.Count > 0;
        foreach (var message in MessageGroups.SelectMany(group => group.Messages))
            message.IsSelectionModeVisible = isVisible;
    }

    private void SetOnline(Guid userId) => UpdateOnlineStatus(userId, true);
    private void SetOffline(Guid userId) => UpdateOnlineStatus(userId, false);

    private void UpdateOnlineStatus(Guid userId, bool online)
    {
        if (_ChatIdForHeader != userId || Header == null) return;

        // SignalR работает в фоне, UI обновляем в MainThread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Header.IsOnline = online;
            Header.Subtitle = _wasOnlineFormater.FormatIsOnline(online);
        });
    }

    private async Task SetUserAvatarAsync(Guid userId, Guid avatarId)
    {
        if (_ChatIdForHeader != userId || Header?.Avatar == null) return;
        
        await MainThread.InvokeOnMainThreadAsync(async () => 
        {
            await Header.Avatar.InitializeAsync(Header.Title, avatarId);
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || !CanWrite) return;

        if (IsEditingMessage && EditingMessage != null)
        {
            EditingMessage.Text = MessageText;
            CancelMessageEditing();
            return;
        }
        
        var textToSend = MessageText;
        MessageText = string.Empty;

        var result = await _controller.SendAsync(ChatId, textToSend);
        
        if (!result.IsSuccess)
        {
            MessageText = textToSend; 
        }
    }

    [RelayCommand]
    public async Task<int> LoadMoreAsync()
    {
        if (IsLoadingMore || !HasMoreMessages)
            return 0;

        IsLoadingMore = true;

        var oldestMessageId = MessageGroups.FirstOrDefault()?.Messages.FirstOrDefault()?.Id;

        if (oldestMessageId is null)
        {
            IsLoadingMore = false;
            return 0;
        }
        
        var result = await _controller.LoadOlderMessagesAsync(ChatId, oldestMessageId);

        if (result.Count == 0)
        {
            HasMoreMessages = false;
        }

        IsLoadingMore = false;
        
        return result.Count;
    }


    // Очистка при закрытии страницы
    public void Dispose()
    {
        UnsubscribeRealtimeEvents();
    }

    [RelayCommand]
    private async Task OpenLink(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    await Launcher.OpenAsync(uri);
                }
                else
                {
                    Console.WriteLine($"Blocked unsafe scheme: {uri.Scheme}");
                }
            }
            else
            {
                Console.WriteLine($"Invalid URL: {url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open link: {ex}");
        }
    }

    private void UnsubscribeRealtimeEvents()
    {
        _realtime.OnUserOnline -= SetOnline;
        _realtime.OnUserOffline -= SetOffline;
        _realtime.OnUserAvatarUpdate -= SetUserAvatarAsync;
    }
}