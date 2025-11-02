using Govor.Mobile.PageModels.AuthFlow;

namespace Govor.Mobile.Pages.AuthFlow;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void NameEntry_Completed(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private async void PasswordEntry_Completed(object sender, EventArgs e)
    {
        if (BindingContext is LoginPageModel vm)
        {
            if (vm.LoginCommand.CanExecute(null))
                await vm.LoginCommand.ExecuteAsync(null);                       
        }
    }
}