namespace Govor.Mobile.Pages.AuthFlow;

public partial class CodeInputPage : ContentPage
{
	public CodeInputPage(CodeInputModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}