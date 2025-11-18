using Govor.Mobile.PageModels.MainFlow;
using static Govor.Mobile.PageModels.MainFlow.SettingsPageModel;
using Syncfusion.Maui.Toolkit.Buttons;

namespace Govor.Mobile.Pages.MainFlow;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

		if(BindingContext is SettingsPageModel vm)
		{
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await vm.Init(); 
            });
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