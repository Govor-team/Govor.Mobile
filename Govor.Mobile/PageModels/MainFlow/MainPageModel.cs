using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class MainPageModel : ObservableObject
{
    [ObservableProperty]
    private string name = "Гость";

    private readonly IProfileCacheService _profileService;

    public void Init()
    {
        // Init user profile
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var profile = await _profileService.GetCurrentAsync();
            Name = profile.DisplayName;
        });
    }

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
