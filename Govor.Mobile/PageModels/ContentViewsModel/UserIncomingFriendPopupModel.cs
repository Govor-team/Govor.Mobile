using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Hubs;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class UserIncomingFriendPopupModel : UserAddNewFriendPopupModel
{
    private readonly Guid _friendship;

    public UserIncomingFriendPopupModel(IFriendsHubService hubService, Guid userId, Guid friendship) 
        : base(hubService, userId)
    {
        _friendship = friendship;
    }
    
    [RelayCommand]
    public async Task AcceptFriendshipAsync()
    {
        var result = await _hubService.AcceptRequestAsync(_friendship);

        await IPopupService.Current.PopAsync();
       
        if (result.Status == HubResultStatus.Success)
            AppShell.DisplayInfo("Успешно!", "Заявка в друзья была успешно одобрена.");
        else
            AppShell.DisplayException("Произошла не предвиденная ошибка!");
    }
    
    [RelayCommand]
    public async Task RejectFriendshipAsync()
    {
        var result = await _hubService.RejectRequestAsync(_friendship);

        await IPopupService.Current.PopAsync();
       
        if (result.Status == HubResultStatus.Success)
            AppShell.DisplayInfo("Успешно!", "Заявка в друзья была отклонена.");
        else
            AppShell.DisplayException("Произошла не предвиденная ошибка!");
    }
}