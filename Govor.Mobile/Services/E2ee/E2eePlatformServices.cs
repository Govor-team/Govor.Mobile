namespace Govor.Mobile.Services.E2ee;

public sealed class E2eeSecureStorage : IE2eeStorage
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
}

public sealed class E2eeRecoveryPrompt : IE2eeRecoveryPrompt
{
    public Task<string?> RequestCodeAsync() => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var page = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null ? null : await page.DisplayPromptAsync("Восстановление истории",
            "Введите код с доверенного устройства (Настройки → Перенос истории) или сохранённый резервный код. " +
            "Он нужен один раз при подключении нового устройства.", "Восстановить", "Позже", maxLength: 64);
    });

    public Task ShowCreatedAsync(string code) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var page = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;
        if (await page.DisplayAlertAsync("Резервный код истории", code +
                "\n\nСохраните код в надёжном месте. Он открывает историю и позволяет подключать устройства. " +
                "Без кода и доверенного устройства восстановить историю нельзя. " +
                "Код также доступен в настройках переноса истории.", "Скопировать", "Сохраню позже"))
            await Clipboard.Default.SetTextAsync(code);
    });
}

