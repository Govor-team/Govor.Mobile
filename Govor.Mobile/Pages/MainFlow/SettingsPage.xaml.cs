using System.Diagnostics;
using Govor.Mobile.Models;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;
using Syncfusion.Maui.Toolkit.Buttons;

namespace Govor.Mobile.Pages.MainFlow;

public partial class SettingsPage : ContentPage
{
	private bool _isInited = false;
	public SettingsPage(SettingsPageModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (!_isInited)
        {
	        if(BindingContext is SettingsPageModel vm)
	        { 
		        await vm.InitAsync();
		        _isInited = true;
	        }
        }
        else
        {
	        if(BindingContext is SettingsPageModel vm)
	        { 
		        await vm.RefreshInit(); 
	        }
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

    private async void OnDeleteBackgroundClicked(object? sender, EventArgs e)
    {
	    if (sender is SfButton btn && btn.CommandParameter is BackgroundItem path)
	    {
		    if (BindingContext is SettingsPageModel vm)
			    await vm.DeleteBackgroundCommand.ExecuteAsync(path);
	    }
    }
}