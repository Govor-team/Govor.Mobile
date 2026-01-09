using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Govor.Mobile.PageModels.ContentViewsModel;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

namespace Govor.Mobile.Pages.ContentViews;

public partial class UserAddNewFriendPopup : PopupPage
{
    public UserAddNewFriendPopup(UserAddNewFriendPopupModel  model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    private async void OnCloseTapped(object? sender, TappedEventArgs e)
    {
       await IPopupService.Current.PopAsync(this);
    }
}