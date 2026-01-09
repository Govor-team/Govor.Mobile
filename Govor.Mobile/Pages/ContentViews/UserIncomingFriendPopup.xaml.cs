using Govor.Mobile.PageModels.ContentViewsModel;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

namespace Govor.Mobile.Pages.ContentViews;

public partial class UserIncomingFriendPopup : PopupPage
{
    public UserIncomingFriendPopup(UserAddNewFriendPopupModel  model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    private async void OnCloseTapped(object? sender, TappedEventArgs e)
    {
       await IPopupService.Current.PopAsync(this);
    }
}