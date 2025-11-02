using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.MainFlow;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class MainPageModel : ObservableObject
{
    [RelayCommand]
    private async Task SettingsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage), false);
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"{ex.Message}");
        }
    }
}
