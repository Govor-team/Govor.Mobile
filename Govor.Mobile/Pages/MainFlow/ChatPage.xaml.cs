using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.MainFlow;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
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
}