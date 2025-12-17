using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.Base;

namespace Govor.Mobile.Pages.MainFlow;

public partial class FriendsSearchPage : AdaptivePage
{
    public FriendsSearchPage(FriendsSearchPageModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected async override void OnAppearing()
    {
        base.OnAppearing();
        
        if(BindingContext is FriendsSearchPageModel vm)
        {
            await vm.InitAsync();
        }
    }

    // For pc
    protected override void OnSwitchToWide()
    {
        LeftCol.Width = new GridLength(1, GridUnitType.Star);
        RightCol.Width = new GridLength(2, GridUnitType.Star);
    }

    protected override void OnSwitchToNarrow()
    {
        LeftCol.Width = new GridLength(1, GridUnitType.Star);
        RightCol.Width = new GridLength(0);
    }
}