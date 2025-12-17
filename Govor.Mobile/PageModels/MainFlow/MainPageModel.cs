using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class MainPageModel : ObservableObject
{
    [ObservableProperty]
    private string name = "Гость";
    
    private readonly IUserProfileService _profileCacheService;

    public MainPageModel(IUserProfileService profileCacheService)
    {
        _profileCacheService = profileCacheService;
    }

    public async Task InitAsync()
    {
        var profile = await _profileCacheService.GetCurrentProfile();
        Name = profile?.Username ?? Name;
    }

    [RelayCommand]
    private async Task OpenFriendsAsync()
    {
        await Shell.Current.GoToAsync("///FriendsSearchPage");
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
            await AppShell.DisplayException($"{ex.Message}");
        }
    }
}
