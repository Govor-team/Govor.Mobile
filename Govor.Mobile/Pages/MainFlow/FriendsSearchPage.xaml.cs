using Govor.Mobile.PageModels.MainFlow;
using UraniumUI.Pages;

namespace Govor.Mobile.Pages.MainFlow;

public partial class FriendsSearchPage : UraniumContentPage
{
    public FriendsSearchPage(FriendsSearchPageModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected async override void OnAppearing()
    {
        base.OnAppearing();
        
        BetterTabView.SelectedIndex = 0;
        
        if(BindingContext is FriendsSearchPageModel vm)
        {
            if(!vm.IsLoaded)
                await vm.InitAsync();
        }
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
    
    private void OnAcceptClicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnDeclineClicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
    
    private void OnSearchFocused(object? sender, EventArgs e)
    {
        if(BindingContext is FriendsSearchPageModel vm)
        {
            vm.IsSearching = true;
        }
    }
}