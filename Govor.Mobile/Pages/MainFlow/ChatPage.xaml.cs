using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage : SmoothBackPage
{
    private bool _isLoadingMore = false;
    private bool _hasMoreMessages = true;
    
    public ChatPage(ChatPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
        
        CollectionView.Scrolled += CollectionView_Scrolled;
    }

    protected override void OnAppearing()
    {
        if (BindingContext is ChatPageModel bc)
        {
            if (!bc.IsLoaded)
                bc.InitAsync();
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
}