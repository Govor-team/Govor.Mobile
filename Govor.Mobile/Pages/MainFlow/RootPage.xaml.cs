using Govor.Mobile.PageModels.MainFlow;
using Sharpnado.Tabs;

namespace Govor.Mobile.Pages.MainFlow;

public partial class RootPage : ContentPage
{
	public RootPage(RootPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
	
	protected override bool OnBackButtonPressed()
	{
		if (BindingContext is RootPageViewModel vm)
		{
			if (vm.IsSettingsOpen)
			{
				vm.IsSettingsOpen = false;
				return true; // отменяем стандартное поведение
			}
		}

		return base.OnBackButtonPressed();
	}
}