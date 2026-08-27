using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.PageModels.MainFlow;
using System.ComponentModel;
using System.Windows.Input;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage : SmoothBackPage
{
    private bool _isLoadingMore = false;
    private bool _hasMoreMessages = true;

    public ICommand LongPressMessageCommand { get; }
    public bool IsMessageSelectionMode { get; private set; }
    
    public ChatPage(ChatPageModel model)
    {
        InitializeComponent();
        LongPressMessageCommand = new Command<MessagesViewModel>(OnMessageLongPressed);
        BindingContext = model;
        model.PropertyChanged += ModelOnPropertyChanged;
        UpdateSelectionModeUi(model.IsSelectionMode);
        
        CollectionView.Scrolled += CollectionView_Scrolled;
    }

    protected override void OnAppearing()
    {
        if (BindingContext is ChatPageModel bc)
        {
            if (!bc.IsLoaded)
                _ = bc.InitAsync();
        }

        base.OnAppearing();
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

        if (BindingContext is ChatPageModel model &&
            CollectionView.ItemsSource is IEnumerable<MessagesGroupModel> source)
        {
            var groups = source.ToList();
            if (groups.Count > 0)
            {
                var firstIndex = Math.Max(0, e.FirstVisibleItemIndex);
                var lastIndex = Math.Min(groups.Count - 1, e.LastVisibleItemIndex);
                if (firstIndex <= lastIndex)
                {
                    var visibleMessages = groups
                        .Skip(firstIndex)
                        .Take(lastIndex - firstIndex + 1)
                        .SelectMany(group => group.Messages)
                        .ToList();

                    await model.MarkMessagesAsReadAsync(visibleMessages);
                }
            }
        }

        if (_isLoadingMore || !_hasMoreMessages)
            return;

        // Когда доскроллил к верхней границе (первые 11 элемента)
        if (e.FirstVisibleItemIndex <= 10)
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
        if (BindingContext is not ChatPageModel model || !model.IsSelectionMode)
            return;

        if (sender is BindableObject view && view.BindingContext is MessagesViewModel message)
            model.ToggleMessageSelectionCommand.Execute(message);
    }

    private void OnMessageLongPressed(MessagesViewModel? message)
    {
        if (message == null || BindingContext is not ChatPageModel model)
            return;

        model.ToggleMessageSelectionCommand.Execute(message);
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChatPageModel.IsSelectionMode))
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsMessageSelectionMode = sender is ChatPageModel model && model.IsSelectionMode;
            UpdateSelectionModeUi(IsMessageSelectionMode);
        });
    }

    private void UpdateSelectionModeUi(bool isSelectionMode)
    {
        ChatHeader.IsVisible = !isSelectionMode;
        SelectionToolbar.IsVisible = isSelectionMode;
        MessageInput.IsVisible = !isSelectionMode;
    }
}