using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;
using Syncfusion.Maui.Toolkit.Buttons;

namespace Govor.Mobile.Pages.MainFlow;

public partial class SettingsPage : ContentPage
{
	private double _totalX;
	private double _totalY;
	public SettingsPage(SettingsPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

		if(BindingContext is SettingsPageModel vm)
		{ 
			await vm.InitAsync(); 
		}
    }

    private async void OnRemoveSessionClicked(object sender, EventArgs e)
    {
        if (sender is SfButton btn && btn.CommandParameter is DeviceSession session)
        {
            if (BindingContext is SettingsPageModel vm)
                await vm.RemoveSessionCommand.ExecuteAsync(session);
        }
    }
}