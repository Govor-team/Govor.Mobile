namespace Govor.Mobile.Pages.Auth_Flow;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}