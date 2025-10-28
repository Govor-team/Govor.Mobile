namespace Govor.Mobile.Pages;

public partial class SomePage : ContentPage
{
	public SomePage(SomePageModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}