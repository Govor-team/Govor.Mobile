using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Hubs;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class UserAddNewFriendPopupModel : ObservableObject
{
    [ObservableProperty]
    private AvatarViewModel avatar;
    
    [ObservableProperty] 
    private TagViewModel tag;

    [ObservableProperty]
    private string title;
    
    [ObservableProperty]
    private string subtitle;
    
    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty] 
    private string onlineText;

    protected readonly Guid _userId;
    protected readonly IFriendsHubService _hubService;
    
    public UserAddNewFriendPopupModel(IFriendsHubService hubService, Guid userId)
    {
        _hubService = hubService;
        _userId = userId;
    }
    
    [RelayCommand(CanExecute = nameof(Can))]
    public async Task SendFriendship()
    {
       var result = await _hubService.SendRequestAsync(_userId);

       await IPopupService.Current.PopAsync();
       
       if (result.Status == HubResultStatus.Created)
           AppShell.DisplayInfo("Успешно!", "Заявка в друзья была успешно отправлена.");
       else
           AppShell.DisplayException("Произошла не предвиденная ошибка!");
    }
    
    private bool Can()
    {
        return true;
    }
}