using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Pages.ContentViews;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.ContentViewsModel
{
    public partial class PopupQrCodeScannerModel : ObservableObject
    {
        private readonly IFriendsHubService _friendsHubService;
        private readonly IUserProfileService _userProfile;
        private readonly IUserListItemViewModelFactory _userListItemViewModelFactory;

        public PopupQrCodeScannerModel(
            IUserListItemViewModelFactory userListItemViewModelFactory,
            IFriendsHubService friendsHubService,
            IUserProfileService userProfile)
        {
            _friendsHubService = friendsHubService;
            _userListItemViewModelFactory = userListItemViewModelFactory;
            _userProfile = userProfile;
        }

        [RelayCommand]
        public async Task HandleQrCodeAsync(string value)
        {
            if (value.ToLower().StartsWith(UserIdQRCodePopupModel.QrContentPrefix))
            {
                var has = Guid.TryParse(
                    value.Substring(UserIdQRCodePopupModel.QrContentPrefix.Length),
                    out Guid parsedId
                );

                if (!has)
                    return;

                await IPopupService.Current.PopAsync();

                var user = await _userListItemViewModelFactory.CreateAsync(parsedId);

                var model = new UserAddNewFriendPopupModel(_friendsHubService, parsedId)
                {
                    Avatar = user.Avatar,
                    Tag = user.Tag,
                    Title = user.Title,
                    Subtitle = user.Subtitle,
                    IsOnline = user.IsOnline,
                    OnlineText = user.DateTime
                };

                var popup = new UserAddNewFriendPopup(model);

                await IPopupService.Current.PushAsync(popup, waitUntilClosed: true);
            }
        }
    }
}
