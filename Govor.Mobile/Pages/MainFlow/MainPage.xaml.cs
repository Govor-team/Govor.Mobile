using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.Base;

namespace Govor.Mobile.Pages.MainFlow;

public partial class MainPage : AdaptivePage
{
    public MainPage(MainPageModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        if(BindingContext is MainPageModel vm)
        {
            
        }

        base.OnAppearing();
    }

    // For pc
    protected override void OnSwitchToWide()
    {
        ChatViewPanel.IsVisible = true;
        LeftCol.Width = new GridLength(1, GridUnitType.Star);
        RightCol.Width = new GridLength(2, GridUnitType.Star);
    }

    protected override void OnSwitchToNarrow()
    {
        ChatViewPanel.IsVisible = false;
        LeftCol.Width = new GridLength(1, GridUnitType.Star);
        RightCol.Width = new GridLength(0);
    }
}