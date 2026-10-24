using System.Diagnostics;
using Govor.Mobile.Models;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;
using Syncfusion.Maui.Toolkit.Buttons;

namespace Govor.Mobile.Pages.MainFlow;

public partial class SettingsPage : SmoothBackPage
{
	private bool _isInited = false;
    private readonly Govor.Mobile.Services.E2ee.E2eeClient _e2ee;
    private readonly Govor.Mobile.Services.Hubs.IHubInitializer _hubs;
	public SettingsPage(SettingsPageModel vm, Govor.Mobile.Services.E2ee.E2eeClient e2ee,
        Govor.Mobile.Services.Hubs.IHubInitializer hubs)
	{
		InitializeComponent();
		BindingContext = vm;
        _e2ee = e2ee;
        _hubs = hubs;
	}

    private async void OnE2eeKeysClicked(object? sender, EventArgs e)
    {
        try
        {
            var key = await _e2ee.GetOwnKeyAsync();
            var accountFingerprint = Govor.Security.E2eeRecoveryProtocol.AccountFingerprint(key.AccountIdentityKey!);
            var fingerprint = string.Join(" ", Enumerable.Range(0, accountFingerprint.Length / 4)
                .Select(i => accountFingerprint.Substring(i * 4, 4)));
            var text = $"Пользователь: {key.UserId}\nСессия: {key.SessionId}\n\n{fingerprint}";
            if (await DisplayAlertAsync("Ключ аккаунта E2EE", text +
                    "\n\nПередайте этот публичный отпечаток собеседнику по независимому доверенному каналу.",
                    "Скопировать", "Закрыть"))
                await Clipboard.Default.SetTextAsync(text);
        }
        catch
        {
            await DisplayAlertAsync("E2EE", "Не удалось загрузить ключ устройства. Проверьте вход и доступность сервера.", "Закрыть");
        }
    }

    private async void OnE2eeRecoveryClicked(object? sender, EventArgs e)
    {
        try
        {
            var code = await _e2ee.GetRecoveryCodeAsync();
            if (await DisplayAlertAsync("Перенос защищённой истории", code +
                    "\n\nВведите этот код на своём новом устройстве после входа. История загрузится в зашифрованном виде. " +
                    "Сохраните его отдельно как резервный код. Любой, кто знает код и имеет доступ к аккаунту, " +
                    "может прочитать историю. Не отправляйте его собеседникам.", "Скопировать", "Закрыть"))
                await Clipboard.Default.SetTextAsync(code);
        }
        catch (Exception ex) { await DisplayAlertAsync("Восстановление E2EE", ex.Message, "Закрыть"); }
    }

    private async void OnE2eeSyncClicked(object? sender, EventArgs e)
    {
        try
        {
            await _e2ee.GetRecoveryCodeAsync();
            await _hubs.ConnectAllAsync();
            await DisplayAlertAsync("E2EE", "Ключи истории синхронизированы. Можно снова открыть чат.", "Закрыть");
        }
        catch (Exception ex) { await DisplayAlertAsync("Синхронизация E2EE", ex.Message, "Закрыть"); }
    }

    private async void OnReactionPacksClicked(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ReactionPacksPage));

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
