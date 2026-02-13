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

    public ChatHeaderService(IServiceProvider provider, IUserProfileService userProfileService, IWasOnlineFormater formater)
    {
        _provider = provider;
        _userProfileService = userProfileService;
        _formater = formater;
    }

    public async Task<ChatHeaderViewModel> BuildAsync(Guid id, bool isGroup, IAsyncRelayCommand backCommand)
    {
        var vm = new ChatHeaderViewModel { IsGroup = isGroup, GoBackCommand = backCommand };

        if (isGroup)
        {
            /*
            var group = await _groupApi.GetGroupInfoAsync(id);
            vm.Title = group.Name;
            vm.AvatarSource = group.AvatarUrl;
            vm.Subtitle = $"{group.MembersCount} участников";
            */
        }
        else
        {
            var profile = await _userProfileService.GetProfileAsync(id);
            
            var avatar = _provider.GetRequiredService<AvatarViewModel>();
            await avatar.InitializeAsync(profile.Username, profile.IconId);
            
            vm.Title = profile.Username;
            
            vm.Avatar = avatar;
            vm.IsOnline = profile.IsOnline;
            vm.Subtitle = _formater.FormatIsOnline(profile.IsOnline);
        }

        return vm;
    }
}