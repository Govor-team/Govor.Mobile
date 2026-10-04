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
[QueryProperty(nameof(PeerIdString), "peerId")]
public partial class ChatPageModel : ObservableObject, IInitializableViewModel, IDisposable
{
    private readonly IMessagesListController _controller;
    private readonly IChatHeaderService _headerService;
    private readonly IFriendsRealtimeService _realtime;
    private readonly IWasOnlineFormater _wasOnlineFormater;
    private readonly IUserProfileService _profileService;
    private readonly IPrivateChatApi _privateChatApi;
    private readonly Govor.Mobile.Services.Api.GroupsApiService _groups;
    private readonly IServerIpProvider _server;
    private readonly Govor.Mobile.Services.Implementations.GroupRealtimeService _groupUpdates;
    private readonly Govor.Mobile.Services.Implementations.GroupMediaService _groupMedia;
    private IDisposable? _groupSubscription;
    private bool _visible;
    public event Action? GroupAccessLost;

    public void StartGroupUpdates()
    {
        _visible = true;
        _groupSubscription?.Dispose();
        if (IsGroup) _groupSubscription = _groupUpdates.Subscribe(ChatId, RefreshLiveGroupAsync);
    }
    public void StopGroupUpdates()
    { _visible = false; _groupSubscription?.Dispose(); _groupSubscription = null; }

    private async Task RefreshLiveGroupAsync(bool reconnect)
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (!_visible) return;
            await RefreshGroupAccessAsync();
            if (reconnect && IsLoaded) await _controller.SyncAsync();
        }
        catch (Govor.Mobile.Services.Api.GroupApiException ex) when
            (ex.Status is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound)
        {
            if (_visible) GroupAccessLost?.Invoke();
        }
        finally { _initializationLock.Release(); }
    }
    public Govor.Mobile.Models.Groups.GroupProfile? GroupProfile { get; private set; }
    public bool CanModerateGroup => Govor.Mobile.Models.Groups.GroupPermissions.CanModerate(GroupProfile);

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

    partial void OnChatIdChanged(Guid value) => ResetChatContext();
    partial void OnIsGroupChanged(bool value) => ResetChatContext();
    private void ResetChatContext()
    {
        IsLoaded = false;
        GroupProfile = null;
        Header = null!;
        MessageGroups.Clear();
        ClearSelection(); CancelMessageEditing();
        StopGroupUpdates();
    }

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
    public bool CanDeleteMessages => SelectedMessages.Count > 0 && (!IsGroup || CanModerateGroup || SelectedMessages.All(m => m.IsOwnMessage));
    public bool CanEditMessage => CanWrite && SelectedMessages.Count == 1 && SelectedMessages[0].IsOwnMessage;

    public ObservableRangeCollection<MessagesGroupModel> MessageGroups => _controller.MessageGroups;
    [ObservableProperty] private ChatHeaderViewModel header;

    public ChatPageModel(
        IMessagesListController controller,
        IWasOnlineFormater wasOnlineFormater,
        IFriendsRealtimeService realtime,
        IUserProfileService profileService,
        IPrivateChatApi privateChatApi,
        IChatHeaderService headerService,
        Govor.Mobile.Services.Api.GroupsApiService groups, IServerIpProvider server,
        Govor.Mobile.Services.Implementations.GroupRealtimeService groupUpdates,
        Govor.Mobile.Services.Implementations.GroupMediaService groupMedia)
    {
        _groups = groups; _server = server; _groupUpdates = groupUpdates; _groupMedia = groupMedia;
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
    private Guid _currentUserId;
    public Task MarkVisibleMessagesReadAsync(int first, int last)
    {
        if (!IsLoaded || first < 0 || last < first) return Task.CompletedTask;
        var ids = MessageGroups.Skip(first).Take(last - first + 1)
            .SelectMany(g => g.Messages).Where(m => !m.IsOwnMessage).Select(m => m.Id).ToArray();
        return _controller.MarkAsReadAsync(_currentUserId, ids);
    }
    private Guid _peerUserId = Guid.Empty;
    public string PeerIdString { set { Guid.TryParse(value, out _peerUserId); } }
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private IAsyncRelayCommand _GoBackCommand { get; }

    public async Task InitAsync()
    {
        await _initializationLock.WaitAsync();
        try { await InitializeCoreAsync(); }
        finally { _initializationLock.Release(); }
    }

    private async Task InitializeCoreAsync()
    {
        if (IsGroup)
            await RefreshGroupAccessAsync();
        else CanWrite = true;
        if (IsLoaded)
            return;

        var profileTask = _profileService.GetCurrentProfileAsync();
        if (!IsGroup && _peerUserId == Guid.Empty)
        {
            var chats = await _privateChatApi.GetPrivateChats();
            _peerUserId = chats.Value?.FirstOrDefault(c => c.ChatId == ChatId)?.FriendId ?? Guid.Empty;
        }
        _ChatIdForHeader = IsGroup ? ChatId : _peerUserId;

        // Теперь header можно строить параллельно с profile
        var headerTask = _headerService.BuildAsync(
            _ChatIdForHeader,
            IsGroup,
            _GoBackCommand);

        await Task.WhenAll(
            headerTask,
            profileTask);

        var profile = await profileTask;

        await MainThread.InvokeOnMainThreadAsync(() => Header = headerTask.Result);

        if (!IsGroup)
        {
            UnsubscribeRealtimeEvents();

            _realtime.OnUserOnline += SetOnline;
            _realtime.OnUserOffline += SetOffline;
            _realtime.OnUserAvatarUpdate += SetUserAvatarAsync;
        }

        if (profile is null)
            throw new InvalidOperationException("Не удалось загрузить профиль пользователя.");
        await _controller.InitializeAsync(
            ChatId,
            profile.Id,
            IsGroup,
            IsGroup && GroupProfile?.IsChannel == true);
        _currentUserId = profile.Id;
        IsLoaded = true;
    }
    public async Task RefreshGroupAccessAsync()
    {
        if (!IsGroup) return;
        try
        {
            GroupProfile = await _groups.GetAsync(ChatId);
            if (GroupProfile.MyRole == null) throw new GroupApiException("Вы больше не участник сообщества.", System.Net.HttpStatusCode.Forbidden);
            CanWrite = Govor.Mobile.Models.Groups.GroupPermissions.CanWrite(GroupProfile);
            if (Header != null)
            {
                Header.Title = GroupProfile.Name;
                Header.Subtitle = GroupProfile.Summary;
                await Header.Avatar.InitializeAsync(GroupProfile.Name, null);
                Header.Avatar.AvatarImage = await _groupMedia.LoadAsync(GroupProfile);
            }
        }
        catch (GroupApiException ex) when (ex.Status is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound)
        {
            GroupProfile = null; CanWrite = false;
            MessageGroups.Clear(); ClearSelection(); CancelMessageEditing();
            if (_controller is IDisposable disposable) disposable.Dispose();
            if (Header != null) Header.Subtitle = "Нет доступа к сообществу";
            throw;
        }
        finally
        {
            OnPropertyChanged(nameof(CanModerateGroup));
            OnPropertyChanged(nameof(CanDeleteMessages));
            OnPropertyChanged(nameof(CanEditMessage));
            EditMessageCommand.NotifyCanExecuteChanged();
            if (!CanWrite) CancelMessageEditing();
        }
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
        OnPropertyChanged(nameof(CanEditMessage));
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
    private async Task DeleteMessages()
    {
        try
        {
            if (!CanDeleteMessages) return;
            if (IsGroup) await RefreshGroupAccessAsync();
            var messageIds = SelectedMessages
                .Select(message => message.Id)
                .ToList();

            foreach (var messageId in messageIds)
            {
                if (IsGroup)
                    await _groups.RemoveMessageAsync(ChatId, messageId);
                else
                    await _controller.RemoveAsync(messageId, forceRemove: true);
            }

            ClearSelection();
        }
        catch (Exception ex)
        {
            if (IsGroup) { try { await RefreshGroupAccessAsync(); } catch { } }
            await Shell.Current.CurrentPage.DisplayAlertAsync("Удаление", ex.Message, "OK");
        }
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
        OnPropertyChanged(nameof(CanEditMessage));
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
            var resEdit = await _controller.EditAsync(EditingMessage.Id, MessageText);

            if (resEdit.IsSuccess)
            {
                EditingMessage.Text = MessageText;
                CancelMessageEditing();
            }
            else if (IsGroup)
            {
                try { await RefreshGroupAccessAsync(); } catch { }
                await Shell.Current.CurrentPage.DisplayAlertAsync("Редактирование", resEdit.ErrorMessage ?? "Не удалось изменить сообщение.", "OK");
            }
            return;
        }

        var textToSend = MessageText;
        MessageText = string.Empty;

        var result = await _controller.SendAsync(ChatId, textToSend);

        if (!result.IsSuccess)
        {
            if (string.IsNullOrEmpty(MessageText)) MessageText = textToSend;
            if (IsGroup)
            {
                try { await RefreshGroupAccessAsync(); } catch { }
                await Shell.Current.CurrentPage.DisplayAlertAsync("Сообщение", result.ErrorMessage ?? "Не удалось отправить сообщение.", "OK");
            }
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
        StopGroupUpdates();
        if (_controller is IDisposable disposable) disposable.Dispose();
        UnsubscribeRealtimeEvents();
    }

    [RelayCommand]
    private async Task OpenLink(string url)
    {
        try
        {
            try
            {
                var code = Govor.Mobile.Utilities.GroupInviteParser.Parse(url, _server.IP);
                await Shell.Current.GoToAsync($"GroupsExplorePage?invite={Uri.EscapeDataString(code)}", false);
                return;
            }
            catch (ArgumentException) { }
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
