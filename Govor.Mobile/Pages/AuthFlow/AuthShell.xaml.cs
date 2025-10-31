namespace Govor.Mobile.Pages.AuthFlow;

public partial class AuthShell : Shell
{
	public AuthShell()
	{
		InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(CodeInputPage), typeof(CodeInputPage));
    }
}