namespace Govor.Mobile.Pages.AuthFlow;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterPageModel vm)
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
        if (BindingContext is RegisterPageModel vm)
        {
            if (vm.NextCommand.CanExecute(null))
                await vm.NextCommand.ExecuteAsync(null);
        }
    }
}