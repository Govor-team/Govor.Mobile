using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.MainFlow;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class BottomBarModel : ObservableObject
{
    [RelayCommand]
    private async Task OpenFriendsAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(FriendsSearchPage)}", animate: true);
        //Shell.Current.CurrentItem = _provider.GetService<FriendsSearchPage>();
    }

    [RelayCommand]
    private async Task OpenChatsAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}", animate: true);
    }

    [RelayCommand]
    private async Task OpenCallsAsync()
    {
        return;
    }
}