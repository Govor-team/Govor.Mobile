using Govor.Mobile.Models.Reactions;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.PageModels.MainFlow;
using System.ComponentModel;
using System.Windows.Input;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage : SmoothBackPage
{
    private bool _pageVisible;
    private int _firstVisible = -1, _lastVisible = -1;
    private bool _isLoadingMore = false;
    private bool _hasMoreMessages = true;

    public ICommand LongPressMessageCommand { get; }
    public ICommand ToggleReactionCommand { get; }
    public bool IsMessageSelectionMode { get; private set; }
    
    public ChatPage(ChatPageModel model, ReactionService reactions,
        GroupMemberContactService contacts, Govor.Mobile.Services.Interfaces.IUserListItemViewModelFactory users)
    {
        ToggleReactionCommand = new Command<ReactionChip>(ToggleReaction);
        InitializeComponent();
        _reactions = reactions; _contacts = contacts; _users = users;
        LongPressMessageCommand = new Command<MessagesViewModel>(OnMessageLongPressed);
        BindingContext = model;
        ChatHeader.SelectionModel = model;
        model.PropertyChanged += ModelOnPropertyChanged;
        model.GroupAccessLost += CloseCommunity;
        UpdateSelectionModeUi(model.IsSelectionMode);
        
        CollectionView.Scrolled += CollectionView_Scrolled;
        model.MessageGroups.CollectionChanged += (_, _) => Dispatcher.Dispatch(ReadVisibleMessages);
        _readTimer = Dispatcher.CreateTimer();
        _readTimer.Interval = TimeSpan.FromSeconds(3);
        _readTimer.Tick += (_, _) => ReadVisibleMessages();
    }

    private readonly IDispatcherTimer _readTimer;
    private async void ReadVisibleMessages()
    {
        if (!_pageVisible || BindingContext is not ChatPageModel model) return;
        try { await model.MarkVisibleMessagesReadAsync(_firstVisible, _lastVisible); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Read acknowledgement failed: {ex.Message}"); }
    }
    protected override void OnDisappearing()
    {
        if (BindingContext is ChatPageModel model) model.StopGroupUpdates();
        _reactions.Changed -= ReactionsChanged;
        _reactions.Reconnected -= ReactionsReconnected;
        if (BindingContext is ChatPageModel m) m.MessageGroups.CollectionChanged -= ReactionGroupsChanged;
        CloseMessageMenu(); CloseUserProfile();
        _pageVisible = false;
        _readTimer.Stop();
        base.OnDisappearing();
    }
    protected override async void OnAppearing()
    {
        _pageVisible = true;
        _reactions.Changed += ReactionsChanged;
        _reactions.Reconnected += ReactionsReconnected;
        if (BindingContext is ChatPageModel m) m.MessageGroups.CollectionChanged += ReactionGroupsChanged;
        _hydrated.Clear();
        HydrateReactions();
        _readTimer.Start();
        if (BindingContext is ChatPageModel bc)
        {
            bc.StartGroupUpdates();
            try { await bc.InitAsync(); }
            catch (Govor.Mobile.Services.Api.GroupApiException ex) when
                (bc.IsGroup && ex.Status is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.NotFound)
            { CloseCommunity(); }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Чат", ex.Message, "OK");
            }
        }

        UpdateSelectionModeUi(BindingContext is ChatPageModel current && current.IsSelectionMode);
        base.OnAppearing();
    }

    private async void CloseCommunity()
    {
        if (!_pageVisible) return;
        CloseMessageMenu();
        if (BindingContext is ChatPageModel model) model.Dispose();
        await Shell.Current.GoToAsync("//root", false);
    }

    private void SendMessageButtonClicked(object? sender, EventArgs e)
    {
        var items = CollectionView.ItemsSource as IList<object>;
        if (items == null || items.Count == 0)
            return;

        var lastItem = items[items.Count - 1];

        CollectionView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
    }
    
    private async void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        _firstVisible = e.FirstVisibleItemIndex;
        _lastVisible = e.LastVisibleItemIndex;
        ReadVisibleMessages();
        RefreshVisibleReactions();
        // Показ/скрытие floating-кнопки "скролл вниз"
        try
        {
            if (CollectionView.ItemsSource is System.Collections.IList items && items.Count > 0)
            {
                var atBottom = e.LastVisibleItemIndex >= items.Count - 1;
                ScrollToEndButton.IsVisible = !atBottom;
            }
            else
            {
                ScrollToEndButton.IsVisible = false;
            }
        }
        catch
        {
            ScrollToEndButton.IsVisible = false;
        }

        if (_isLoadingMore || !_hasMoreMessages)
            return;

        // Когда доскроллил к верхней границе (первые 11 элемента)
        if (e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex <= 10)
        {
            _isLoadingMore = true;

            if (BindingContext is ChatPageModel bc)
            {
                // Сохраняем первый видимый элемент
                var firstIndex = e.FirstVisibleItemIndex;
                if (CollectionView.ItemsSource is IList<MessagesGroupModel> messages && messages.Count > firstIndex)
                {
                    var element = messages[firstIndex];
                    var loadedCount = await bc.LoadMoreAsync();

                    // если новых сообщений не пришло — отключаем дальнейшую подгрузку
                    if (loadedCount == 0)
                        _hasMoreMessages = false;
                    
                    CollectionView.ScrollTo(element, position: ScrollToPosition.Start, animate: false);
                }
            }

            _isLoadingMore = false;
        }
    }

    private void ScrollToBottomButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (CollectionView.ItemsSource is System.Collections.IList items && items.Count > 0)
            {
                var last = items[items.Count - 1];
                CollectionView.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                ScrollToEndButton.IsVisible = false;
            }
        }
        catch
        {
            // ignore
        }
    }

    private void MarkdownView_OnHyperLinkClicked(object sender, Indiko.Maui.Controls.Markdown.LinkEventArgs e)
    {
        if (BindingContext is ChatPageModel bc)
        {
            if (bc.IsLoaded)
                bc.OpenLinkCommand.Execute(e.Url);
        }
    }
    
    private void MessageDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not ChatPageModel model)
            return;

        if (sender is BindableObject view &&
            view.BindingContext is MessagesViewModel message)
        {
            model.ToggleMessageSelectionCommand.Execute(message);
        }
    }

    private void MessageTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not ChatPageModel model) return;
        if (sender is BindableObject view && view.BindingContext is MessagesViewModel message)
        {
            if (model.IsSelectionMode) model.ToggleMessageSelectionCommand.Execute(message);
            else ShowMessageMenu(message);
        }
    }

    private void OnMessageLongPressed(MessagesViewModel? message)
    {
        if (message == null || BindingContext is not ChatPageModel model)
            return;

        model.ToggleMessageSelectionCommand.Execute(message);
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChatPageModel.IsSelectionMode) && e.PropertyName != nameof(ChatPageModel.CanWrite) && e.PropertyName != nameof(ChatPageModel.CanModerateGroup))
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.PropertyName == nameof(ChatPageModel.CanWrite) || e.PropertyName == nameof(ChatPageModel.CanModerateGroup)) CloseMessageMenu();
            IsMessageSelectionMode = sender is ChatPageModel model && model.IsSelectionMode;
            UpdateSelectionModeUi(IsMessageSelectionMode);
        });
    }

    private void UpdateSelectionModeUi(bool isSelectionMode)
    {
        MessageInput.IsVisible = !isSelectionMode && (BindingContext is not ChatPageModel model || model.CanWrite);
    }
}
