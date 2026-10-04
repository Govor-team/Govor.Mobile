using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;

namespace Govor.Mobile.Pages.MainFlow;

public partial class MainShell : Shell
{
    private readonly IUserProfileDonloaderSerivce _profileService;
    private readonly ProfileHubListener _listener; // Слушает хаб, должен работать всегда 
    
    public MainShell(ProfileHubListener listener)
    {
        InitializeComponent();
        
        _listener = listener;

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute("chat", typeof(ChatPage));
        Routing.RegisterRoute("memberChat", typeof(ChatPage));
        Routing.RegisterRoute(nameof(ReactionPacksPage), typeof(ReactionPacksPage));
        Routing.RegisterRoute(nameof(GroupsExplorePage), typeof(GroupsExplorePage));
        Routing.RegisterRoute(nameof(GroupProfilePage), typeof(GroupProfilePage));
    }
}
