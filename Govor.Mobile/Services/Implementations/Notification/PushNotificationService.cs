#if ANDROID
using Android.App;
using Android.Content;

#endif
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces.Notification;
using Plugin.FirebasePushNotifications;
using Plugin.FirebasePushNotifications.Model;

namespace Govor.Mobile.Services.Implementations.Notification;

public class PushNotificationService : IPushNotificationService, IConnectivityChanged
{
    private readonly IFirebasePushNotification _firebase;
    private readonly IPushTokenService _pushTokenService;
    private readonly INotificationPermissions _permissions;
    private readonly INotificationChannels _channels;

    private bool _isInitialized;
    private bool _eventsSubscribed;
    private static bool _channelCreated = false;

    private readonly SemaphoreSlim _initLock = new(1, 1);

    private const string TokenCacheKey = "fcm_token_sent";

    public PushNotificationService(
        IFirebasePushNotification firebase,
        INotificationPermissions permissions,
        IPushTokenService pushTokenService,
        INotificationChannels channels)
    {
        _firebase = firebase;
        _permissions = permissions;
        _pushTokenService = pushTokenService;
        _channels = channels;
    }

    public async Task OnInternetConnectedAsync()
    {
        await InitializeAsync();
    }

    public Task OnInternetDisconnectedAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            Console.WriteLine("[Push] Initializing...");

            // 1. Permissions
            var status = await _permissions.GetAuthorizationStatusAsync();
            if (status is AuthorizationStatus.NotDetermined or AuthorizationStatus.Denied)
            {
                await _permissions.RequestPermissionAsync();
            }

            // 2. Subscribe events (idempotent)
            if (!_eventsSubscribed)
            {
                _firebase.TokenRefreshed += OnTokenRefreshed;
                _firebase.NotificationReceived += OnNotificationReceived;
                _firebase.NotificationOpened += OnNotificationOpened;

                _eventsSubscribed = true;
            }

            // 3. Android channel
#if ANDROID
            var channel = new Plugin.FirebasePushNotifications.Platforms.Channels.NotificationChannelRequest
            {
                ChannelId = "chat_messages",
                ChannelName = "Сообщения",
                Description = "Уведомления о сообщениях",
                Importance = NotificationImportance.High,
                LockscreenVisibility = NotificationVisibility.Public,
                VibrationPattern = new long[] { 0, 250, 250, 250 },
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var manager = (NotificationManager)Android.App.Application.Context
                    .GetSystemService(Context.NotificationService);

                var existing = manager.GetNotificationChannel("chat_messages");

                if (existing == null)
                {
                    var channel = new NotificationChannel(
                        "chat_messages",
                        "Сообщения",
                        NotificationImportance.High)
                    {
                        Description = "Уведомления о сообщениях"
                    };

                    manager.CreateNotificationChannel(channel);

                    Console.WriteLine("[Push] Channel created");
                }
                else
                {
                    Console.WriteLine("[Push] Channel already exists");
                }
            });
#endif

            // 4. Register in FCM
            await _firebase.RegisterForPushNotificationsAsync();

            // 5. Ensure token delivery (critical fix)
            await EnsureTokenSentAsync();

            _isInitialized = true;

            Console.WriteLine("[Push] Initialized");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[Push] Error - {ex}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureTokenSentAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            var token = _firebase.Token;

            if (!string.IsNullOrWhiteSpace(token))
            {
                await SendTokenIfNeededAsync(token);
                return;
            }

            await Task.Delay(1000);
        }

        Console.WriteLine("[Push] Token not available after retries");
    }

    private async Task SendTokenIfNeededAsync(string token)
    {
        var lastSent = Preferences.Get(TokenCacheKey, string.Empty);

        if (lastSent == token)
        {
            Console.WriteLine("[Push] Token already sent, skipping");
            return;
        }

        await SendTokenToServerAsync(token);

        Preferences.Set(TokenCacheKey, token);
    }

    private async Task SendTokenToServerAsync(string token)
    {
        try
        {
            Console.WriteLine($"[Push] Sending token: {token}");

            var result = await _pushTokenService.PushToken(
                token,
                DeviceInfo.Platform.ToString().ToLower()
            );

            Console.WriteLine($"[Push] Token result: success={result.IsSuccess}, error={result.ErrorMessage}");

            if (!result.IsSuccess)
            {
                // retry позже (не блокируем UI)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await SendTokenToServerAsync(token);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Push] Send token failed: {ex}");

            // retry
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                await SendTokenToServerAsync(token);
            });
        }
    }

    private void OnTokenRefreshed(object sender, FirebasePushNotificationTokenEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(e.Token))
                {
                    Console.WriteLine($"[Push] Token refreshed: {e.Token}");
                    await SendTokenIfNeededAsync(e.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Push] Token refresh error: {ex}");
            }
        });
    }

    private void OnNotificationReceived(object sender, FirebasePushNotificationDataEventArgs e)
    {
        Console.WriteLine("[Push] Notification received");

        if (e.Data.TryGetValue("chatId", out var chatId))
        {
            Console.WriteLine($"[Push] chatId: {chatId}");
        }
    }

    private void OnNotificationOpened(object sender, FirebasePushNotificationResponseEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (e.Data.TryGetValue("chatId", out var chatIdStr) &&
                    Guid.TryParse(chatIdStr?.ToString(), out var chatId))
                {
                    var isGroup = e.Data.TryGetValue("isGroup", out var isGroupStr) &&
                                  bool.TryParse(isGroupStr?.ToString(), out var g) && g;

                    Console.WriteLine($"[Push] Open chat {chatId}");

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync(
                            $"chat?chatId={chatId}&isGroup={isGroup}",
                            animate: false);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Push] Open notification error: {ex}");
            }
        });
    }

    public async Task UnregisterAsync()
    {
        if (!_isInitialized)
            return;

        Console.WriteLine("[Push] Unregistering");

        if (_eventsSubscribed)
        {
            _firebase.TokenRefreshed -= OnTokenRefreshed;
            _firebase.NotificationReceived -= OnNotificationReceived;
            _firebase.NotificationOpened -= OnNotificationOpened;

            _eventsSubscribed = false;
        }

        await _firebase.UnregisterForPushNotificationsAsync();

        Preferences.Remove(TokenCacheKey);

        _isInitialized = false;
    }
}