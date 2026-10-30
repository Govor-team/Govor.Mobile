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
        return page is null ? null : await GlassDialogs.PromptAsync("Восстановление истории",
            "Для этой старой истории ещё нет доступной парольной копии ключа. Введите код с доверенного устройства " +
            "или резервный код. Если они потеряны, нажмите «Позже», затем в настройках выберите " +
            "«Начать общение с новыми ключами». Старая история при этом не восстановится.", "Восстановить", "Позже", maxLength: 64);
    });

    public Task ShowCreatedAsync(string code) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var page = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;
        if (await GlassDialogs.DisplayAlertAsync("Резервный код истории", code +
                "\n\nСохраните код в надёжном месте. Он открывает историю и позволяет подключать устройства. " +
                "Обычно история восстанавливается по паролю; код — запасной способ на случай забытого пароля. " +
                "Код также доступен в настройках переноса истории.", "Скопировать", "Сохраню позже"))
            await Clipboard.Default.SetTextAsync(code);
    });
}

public sealed class PasswordAccountPrompts : IPasswordAccountPrompts
{
    public Task<string?> UpgradePasswordAsync() => PasswordPrompt.ShowAsync("Обновление входа",
        "Аккаунт использует прежний способ входа. Прежний пароль будет проверен старым серверным методом. " +
        "Выберите НОВЫЙ пароль: после перехода новый пароль не передаётся серверу. " +
        "Если вы уже обновляли вход, отмените действие и проверьте адрес сервера.", true);
    public Task<string?> CurrentPasswordAsync(string reason) => PasswordPrompt.ShowAsync(reason, "Введите текущий пароль аккаунта.");
    public Task NotifyIdentityChangedAsync(Guid user) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var page = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null) await GlassDialogs.DisplayAlertAsync("Собеседник начал с новыми ключами",
            $"Аккаунт {user} сбросил защиту переписки после потери доступа. Его новые сообщения используют новый ключ аккаунта. " +
            "Ранее полученная вами история сохраняется.", "Понятно");
    });
}

public static class PasswordPrompt
{
    public static Task<string?> ShowAsync(string title, string description, bool confirm = false) =>
        GlassDialogs.PromptAsync(title, description, password: true, confirm: confirm);
}