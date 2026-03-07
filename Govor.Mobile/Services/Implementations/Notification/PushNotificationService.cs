#if ANDROID
using Android.App;
#endif
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces.Notification;
using Plugin.FirebasePushNotifications;
using Plugin.FirebasePushNotifications.Model;

namespace Govor.Mobile.Services.Implementations.Notification;

public class PushNotificationService : IPushNotificationService, IConnectivityChanged
{
    private readonly IApiClient _apiClient;
    private readonly IFirebasePushNotification _firebase;
    private readonly IPushTokenService _pushTokenService;
    private readonly INotificationPermissions _permissions;
    private readonly INotificationChannels _channels; // для Android каналов
    private bool _isInitialized = false;
    private const string _pref_key = "FBT_Govor";

    public PushNotificationService(
        IApiClient apiClient,
        IFirebasePushNotification firebase,
        INotificationPermissions permissions,
        IPushTokenService pushTokenService,
        INotificationChannels channels)
    {
        _apiClient = apiClient;
        _firebase = firebase;
        _permissions = permissions;
        _pushTokenService = pushTokenService;
        _channels = channels;
    }

    public async Task OnInternetConnectedAsync()
    {
        await InitializeAsync();
    }

    public async Task OnInternetDisconnectedAsync() { }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return; 
        
        // 1. Запрос разрешений (лучше после логина пользователя)
        var status = await _permissions.GetAuthorizationStatusAsync();
        if (status is AuthorizationStatus.NotDetermined or AuthorizationStatus.Denied)
        {
            await _permissions.RequestPermissionAsync();
        }

        // 2. Подписка на события (один раз)
        _firebase.TokenRefreshed += OnTokenRefreshed;
        _firebase.NotificationReceived += OnNotificationReceived;
        _firebase.NotificationOpened += OnNotificationOpened;

        // 3. Создание канала уведомлений (только для Android 8+)
#if ANDROID
        var chatChannel = new Plugin.FirebasePushNotifications.Platforms.Channels.NotificationChannelRequest
        {
            ChannelId = "chat_messages",
            ChannelName = "Сообщения",
            Description = "Уведомления о новых сообщениях в чатах",
            Importance = NotificationImportance.High,
            Group = "Govor",
            LockscreenVisibility = NotificationVisibility.Public,
            VibrationPattern = new long[] { 0, 250, 250, 250 },
            // Sound = "chat_sound" // если есть кастомный звук
        };

        // Передаём как массив даже если канал один
        _channels.CreateNotificationChannels(new[]
        {
            chatChannel
        });

        // или в новых версиях плагина может быть CreateChannelAsync для одного
        // await _channels.CreateChannelAsync(chatChannel);
#endif

        // 3. Регистрируемся в FCM с retry
        await _firebase.RegisterForPushNotificationsAsync();

        // 4. Если токен уже есть — сразу отправляем на сервер
        if (!string.IsNullOrEmpty(_firebase.Token))
        {
            Console.WriteLine($"[TOKEN]: {_firebase.Token}");
            await SendTokenToServerAsync(_firebase.Token);
        }
        
        _isInitialized = true;
    }

    private async void OnTokenRefreshed(object sender, FirebasePushNotificationTokenEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Token))
            await SendTokenToServerAsync(e.Token);
    }

    private async Task SendTokenToServerAsync(string token)
    {
        try
        {
            if (Preferences.Get(_pref_key, "") != token)
            {
                await _pushTokenService.PushToken(token, DeviceInfo.Platform.ToString().ToLower());
                Preferences.Set(_pref_key, token);
            }
              
            // Можно сохранить токен локально Preferences.Set("fcm_token", token);
        }
        catch (Exception ex)
        {
            // логирование
        }
    }

    private void OnNotificationReceived(object sender, FirebasePushNotificationDataEventArgs e)
    {
        // Уведомление пришло (foreground или background)
        // Если это data-only сообщение — можно обработать тихо (обновить чат без баннера)

        if (e.Data.TryGetValue("chatId", out var chatId))
        {
            // Например, показать in-app баннер или обновить badge
            // MessagingCenter.Send(this, "NewMessage", chatId);
        }
    }

    private async void OnNotificationOpened(object sender, FirebasePushNotificationResponseEventArgs e)
    {
        // Пользователь тапнул по уведомлению
        if (e.Data.TryGetValue("chatId", out var chatIdObj) && chatIdObj is Guid chatId)
        {
            if (e.Data.TryGetValue("isGroup", out var isGroupObj) && isGroupObj is bool isGroup)
            {
                await Shell.Current.GoToAsync($"chat?chatId={chatId}&isGroup={isGroup}", animate: false);
            }
        }
    }

    // При логауте — обязательно!
    public async Task UnregisterAsync()
    {
        if(!_isInitialized)
            return;
        
        _firebase.TokenRefreshed -= OnTokenRefreshed;
        _firebase.NotificationReceived -= OnNotificationReceived;
        _firebase.NotificationOpened -= OnNotificationOpened;

        await _firebase.UnregisterForPushNotificationsAsync();
        _isInitialized = false;
    }
}