using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.ChatPage;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.ChatPage;

public class ChatHeaderService : IChatHeaderService
{
    private readonly IServiceProvider _provider;
    private readonly IUserProfileService _userProfileService;
    private readonly IWasOnlineFormater _formater;
    //private readonly IGroupApiClient _groupApi;
    //private readonly IOnlineUserStore _onlineStore;

    public ChatHeaderService(
        IServiceProvider provider,
        IUserProfileService userProfileService,
        IWasOnlineFormater formater)
    {
        _provider = provider;
        _userProfileService = userProfileService;
        _formater = formater;
    }

    public async Task<ChatHeaderViewModel> BuildAsync(Guid id, bool isGroup, IAsyncRelayCommand backCommand)
    {
        var vm = new ChatHeaderViewModel { IsGroup = isGroup, GoBackCommand = backCommand, Id = id };

        if (isGroup)
        {
            var avatar = _provider.GetRequiredService<AvatarViewModel>();
            await avatar.InitializeAsync("Групповой чат", null);

            vm.Title = "Групповой чат";
            vm.Subtitle = "Группа";
            vm.Avatar = avatar;
        }
        else
        {
            var profile = await _userProfileService.GetProfileAsync(id);
            
            var avatar = _provider.GetRequiredService<AvatarViewModel>();
            await avatar.InitializeAsync(profile?.Username ?? "Чат", profile?.IconId);
            
            vm.Title = profile?.Username ?? "Чат";
            
            vm.Avatar = avatar;
            vm.IsOnline = profile?.IsOnline ?? false;
            vm.Subtitle = _formater.FormatIsOnline(vm.IsOnline);
        }

        // Default leave command - navigate back. Consumers can override if needed.
        vm.LeaveCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync("..", true);
            }
            catch
            {
                // swallow navigation errors
            }
        });
        
        return vm;
    }
}
