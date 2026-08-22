using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.MainFlow;

public partial class RootPage : ContentPage
{
	public RootPage(RootPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}