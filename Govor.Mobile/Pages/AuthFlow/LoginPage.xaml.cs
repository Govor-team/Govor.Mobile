using Govor.Mobile.PageModels.AuthFlow;

namespace Govor.Mobile.Pages.AuthFlow;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}