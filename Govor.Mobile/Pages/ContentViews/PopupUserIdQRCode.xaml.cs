using Govor.Mobile.PageModels.ContentViewsModel;
using UXDivers.Popups.Maui;

namespace Govor.Mobile.Pages.ContentViews;

public partial class PopupUserIdQRCode : PopupPage
{
	public PopupUserIdQRCode(UserIdQRCodePopupModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}