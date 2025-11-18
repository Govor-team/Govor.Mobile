using Govor.Mobile.Services.Api;

namespace Govor.Mobile.Pages.MainFlow;

public partial class MainShell : Shell
{
    private readonly IUserProfileDonloaderSerivce _profileService;

    public MainShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadProfileAsync();
        });
    }

    private async Task LoadProfileAsync()
    {

    }
}
