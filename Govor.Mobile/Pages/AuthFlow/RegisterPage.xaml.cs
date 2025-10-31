namespace Govor.Mobile.Pages.AuthFlow;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}