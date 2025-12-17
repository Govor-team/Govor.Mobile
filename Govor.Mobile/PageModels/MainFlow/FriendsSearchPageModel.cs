using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class FriendsSearchPageModel : ObservableObject, IDisposable
{
    public void Dispose()
    {
        // TODO release managed resources here
    }

    public async Task InitAsync()
    {
        
    }

    [RelayCommand]
    private async Task OpenChatsAsync()
    {
        await Shell.Current.GoToAsync("///MainPage");
    }
}