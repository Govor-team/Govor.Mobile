namespace Govor.Mobile.Pages.Auth_Flow;

public partial class CodeInputPage : ContentPage
{
	public CodeInputPage(CodeInputModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}