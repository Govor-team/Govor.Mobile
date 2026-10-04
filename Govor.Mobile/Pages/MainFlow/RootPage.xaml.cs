using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.MainFlow;

public partial class RootPage : ContentPage
{
	public RootPage(RootPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RootPageViewModel model)
            await model.HomePageViewModel.Groups.RefreshAsync();
    }
}