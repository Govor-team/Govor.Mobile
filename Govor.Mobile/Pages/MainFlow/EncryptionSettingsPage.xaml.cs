namespace Govor.Mobile.Pages.MainFlow;

public partial class EncryptionSettingsPage : SmoothBackPage
{
    private readonly Govor.Mobile.Services.E2ee.E2eeClient _e2ee;
    private readonly Govor.Mobile.Services.Hubs.IHubInitializer _hubs;

    public EncryptionSettingsPage(Govor.Mobile.Services.E2ee.E2eeClient e2ee,
        Govor.Mobile.Services.Hubs.IHubInitializer hubs)
    {
        InitializeComponent();
        _e2ee = e2ee;
        _hubs = hubs;
    }

    private async void CloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnE2eeKeysClicked(object? sender, EventArgs e)
    {
        if (!Actions.IsEnabled) return;
        Actions.IsEnabled = false;
        try
        {
            var key = await _e2ee.GetOwnKeyAsync();
            var accountFingerprint = Govor.Security.E2eeRecoveryProtocol.AccountFingerprint(key.AccountIdentityKey!);
            var fingerprint = string.Join(" ", Enumerable.Range(0, accountFingerprint.Length / 4)
                .Select(i => accountFingerprint.Substring(i * 4, 4)));
            var text = $"Пользователь: {key.UserId}\nСессия: {key.SessionId}\n\n{fingerprint}";
            if (await GlassDialogs.DisplayAlertAsync("Ключ аккаунта E2EE", text +
                    "\n\nПередайте этот публичный отпечаток собеседнику по независимому доверенному каналу.",
                    "Скопировать", "Закрыть"))
                await Clipboard.Default.SetTextAsync(text);
        }
        catch
        {
            await GlassDialogs.DisplayAlertAsync("E2EE", "Не удалось загрузить ключ устройства. Проверьте вход и доступность сервера.", "Закрыть");
        }
        finally { Actions.IsEnabled = true; }
    }

    private async void OnE2eeRecoveryClicked(object? sender, EventArgs e)
    {
        if (!Actions.IsEnabled) return;
        Actions.IsEnabled = false;
        try
        {
            var code = await _e2ee.GetRecoveryCodeAsync();
            if (await GlassDialogs.DisplayAlertAsync("Перенос защищённой истории", code +
                    "\n\nОбычно на новом устройстве достаточно войти по паролю. Этот код — запасной способ " +
                    "переноса истории и восстановления после забытого пароля. Сохраните его отдельно. Любой, кто знает код и имеет доступ к аккаунту, " +
                    "может прочитать историю. Не отправляйте его собеседникам.", "Скопировать", "Закрыть"))
                await Clipboard.Default.SetTextAsync(code);
        }
        catch (Exception ex) { await GlassDialogs.DisplayAlertAsync("Восстановление E2EE", ex.Message, "Закрыть"); }
        finally { Actions.IsEnabled = true; }
    }

    private async void OnE2eeSyncClicked(object? sender, EventArgs e)
    {
        if (!Actions.IsEnabled) return;
        Actions.IsEnabled = false;
        try
        {
            await _e2ee.GetRecoveryCodeAsync();
            await _hubs.ConnectAllAsync();
            await GlassDialogs.DisplayAlertAsync("E2EE", "Ключи истории синхронизированы. Можно снова открыть чат.", "Закрыть");
        }
        catch (Exception ex) { await GlassDialogs.DisplayAlertAsync("Синхронизация E2EE", ex.Message, "Закрыть"); }
        finally { Actions.IsEnabled = true; }
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        if (!Actions.IsEnabled) return;
        Actions.IsEnabled = false;
        try
        {
            var oldPassword = await Govor.Mobile.Services.E2ee.PasswordPrompt.ShowAsync("Смена пароля", "Введите текущий пароль.");
            if (oldPassword is null) return;
            var newPassword = await Govor.Mobile.Services.E2ee.PasswordPrompt.ShowAsync("Новый пароль", "История будет сохранена. Остальные сессии будут завершены.", true);
            if (newPassword is null) return;
            await _e2ee.ChangePasswordAsync(oldPassword, newPassword);
            await GlassDialogs.DisplayAlertAsync("Пароль изменён", "История сохранена. На других устройствах войдите с новым паролем.", "Понятно");
        }
        catch (Exception ex) { await GlassDialogs.DisplayAlertAsync("Смена пароля", ex.Message, "Закрыть"); }
        finally { Actions.IsEnabled = true; }
    }

    private async void OnResetEncryptionClicked(object? sender, EventArgs e)
    {
        if (!Actions.IsEnabled) return;
        Actions.IsEnabled = false;
        try
        {
        if (!await GlassDialogs.DisplayAlertAsync("Начать с новыми ключами?",
                "Используйте это только при потере всех способов восстановления. Старая зашифрованная история " +
                "не восстановится на этом устройстве. Остальные сессии завершатся, а собеседники получат уведомление о смене ключей.",
                "Начать заново", "Отмена")) return;
            await _e2ee.ResetEncryptionAsync();
            await _e2ee.InitializeAsync();
            await _hubs.ConnectAllAsync();
            await GlassDialogs.DisplayAlertAsync("Новые ключи готовы", "Можно продолжать общение. В дальнейшем история восстанавливается по текущему паролю.", "Понятно");
        }
        catch (Exception ex) { await GlassDialogs.DisplayAlertAsync("Сброс шифрования", ex.Message, "Закрыть"); }
        finally { Actions.IsEnabled = true; }
    }


}
