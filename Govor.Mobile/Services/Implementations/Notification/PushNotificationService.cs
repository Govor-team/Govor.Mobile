using Govor.Mobile.Models;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Services.Interfaces.Notification;
using Plugin.FirebasePushNotifications;
using Plugin.FirebasePushNotifications.Model;

namespace Govor.Mobile.Services.Implementations.Notification;

public sealed class PushNotificationService : IPushNotificationService, IConnectivityChanged
{
    private readonly IFirebasePushNotification _firebase;
    private readonly INotificationPermissions _permissions;
    private readonly IJwtProviderService _session;
    private readonly PushTokenRegistration _registration;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<(ChatPushNotification Notification, Guid? User)> _pending = new();
    private readonly object _historyLock = new();
    private readonly object _retryLock = new();
    private bool _initialized;
    private bool _shellReady;
    private CancellationTokenSource? _retry;
    private const int HistoryLimit = PushMessageHistory.Limit;

    public PushNotificationService(IFirebasePushNotification firebase, INotificationPermissions permissions,
        IPushTokenService api, IJwtProviderService session)
    {
        _firebase = firebase;
        _permissions = permissions;
        _session = session;
        _registration = new(session, api);
        // Subscribe before startup/auth restoration so cold-start taps are not lost.
        _firebase.TokenRefreshed += OnTokenRefreshed;
        _firebase.NotificationReceived += OnNotificationReceived;
        _firebase.NotificationOpened += OnNotificationOpened;
        _session.WasClearTokens += ClearPending;
        Preferences.Remove("fcm_token_sent");
    }

    public Task OnInternetConnectedAsync() => InitializeAsync();
    public Task OnInternetDisconnectedAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
#if ANDROID || IOS
        await _gate.WaitAsync();
        try
        {
            if (!_session.HasRefreshToken) return;
            var status = await MainThread.InvokeOnMainThreadAsync(() => _permissions.GetAuthorizationStatusAsync());
            if (status == AuthorizationStatus.NotDetermined)
            {
                await MainThread.InvokeOnMainThreadAsync(() => _permissions.RequestPermissionAsync());
                status = await _permissions.GetAuthorizationStatusAsync();
            }
            if (status != AuthorizationStatus.Granted) return;
            if (!_initialized)
            {
                await MainThread.InvokeOnMainThreadAsync(() => _firebase.RegisterForPushNotificationsAsync());
                _initialized = true;
            }
            // TokenRefreshed handles tokens that become available later.
            if (!string.IsNullOrWhiteSpace(_firebase.Token) &&
                !await _registration.RegisterAsync(_firebase.Token, DeviceInfo.Platform.ToString()))
                ScheduleRetry();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Push registration failed: {ex.Message}");
            ScheduleRetry();
        }
        finally { _gate.Release(); }
#endif
        await OpenPendingAsync();
    }

    public async Task SetShellReadyAsync(bool ready)
    {
        _shellReady = ready;
        if (!ready) ClearPending();
        else
        {
            try { await OpenPendingAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Pending push navigation failed: {ex.Message}"); }
        }
    }

    private void ClearPending()
    {
        _shellReady = false;
        lock (_retryLock) { _retry?.Cancel(); }
        lock (_pending) { _pending.Clear(); }
    }

    private async void OnTokenRefreshed(object? sender, FirebasePushNotificationTokenEventArgs e)
    {
        await _gate.WaitAsync();
        try
        {
#if ANDROID || IOS
            if (_session.HasRefreshToken && !string.IsNullOrWhiteSpace(e.Token) &&
                await _permissions.GetAuthorizationStatusAsync() == AuthorizationStatus.Granted &&
                !await _registration.RegisterAsync(e.Token, DeviceInfo.Platform.ToString())) ScheduleRetry();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Push token refresh failed: {ex.Message}");
            ScheduleRetry();
        }
        finally { _gate.Release(); }
    }

    // Retry transient registration failures even when connectivity does not change.
    private void ScheduleRetry()
    {
        CancellationTokenSource retry;
        lock (_retryLock)
        {
            if (!_session.HasRefreshToken || _retry is { IsCancellationRequested: false }) return;
            _retry = retry = new CancellationTokenSource();
        }
        _ = RetryRegistrationAsync(retry);
    }

    private async Task RetryRegistrationAsync(CancellationTokenSource retry)
    {
        try
        {
            for (var delay = 2; ; delay = Math.Min(delay * 2, 60))
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), retry.Token);
                await _gate.WaitAsync(retry.Token);
                try
                {
                    if (!_session.HasRefreshToken) return;
                    if (await _permissions.GetAuthorizationStatusAsync() != AuthorizationStatus.Granted) return;
                    if (!_initialized)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => _firebase.RegisterForPushNotificationsAsync());
                        _initialized = true;
                    }
                    if (!string.IsNullOrWhiteSpace(_firebase.Token) &&
                        await _registration.RegisterAsync(_firebase.Token, DeviceInfo.Platform.ToString())) return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { System.Diagnostics.Debug.WriteLine($"Push registration retry failed: {ex.Message}"); }
                finally { _gate.Release(); }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_retryLock)
            {
                if (ReferenceEquals(_retry, retry)) _retry = null;
                retry.Dispose();
            }
        }
    }

    private void OnNotificationReceived(object? sender, FirebasePushNotificationDataEventArgs e)
    {
        if (!ChatPushNotification.TryParse(e.Data, out var notification)) return;
        if (_session.CurrentUserId is not Guid user) return;
        lock (_historyLock)
        {
            var key = $"push_received_messages_{user:N}";
            if (!PushMessageHistory.TryRemember(Preferences.Get(key, "[]"), notification!.MessageId, out var history)) return;
            Preferences.Set(key, history);
        }
        // Delivery is not a read acknowledgement. The chat viewport owns read receipts.
        // Do not generate another local notification: FCM owns the system notification.
    }

    private async void OnNotificationOpened(object? sender, FirebasePushNotificationResponseEventArgs e)
    {
        try
        {
            if (!ChatPushNotification.TryParse(e.Data, out var notification)) return;
            lock (_pending)
            {
                if (_pending.Count >= HistoryLimit) _pending.Dequeue();
                _pending.Enqueue((notification!, _session.CurrentUserId));
            }
            await OpenPendingAsync();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Push navigation failed: {ex.Message}"); }
    }

    private async Task OpenPendingAsync()
    {
        await _gate.WaitAsync();
        try
        {
            while (_shellReady && _session.HasRefreshToken && _session.CurrentUserId is Guid user)
            {
                (ChatPushNotification Notification, Guid? User) item;
                lock (_pending)
                {
                    if (!_pending.TryPeek(out item)) return;
                }
                if (item.User is Guid owner && owner != user)
                {
                    lock (_pending) { if (_pending.Count > 0 && _pending.Peek() == item) _pending.Dequeue(); }
                    continue;
                }
                var key = $"push_opened_messages_{user:N}";
                if (PushMessageHistory.TryRemember(Preferences.Get(key, "[]"), item.Notification.MessageId, out var history))
                {
                    var opened = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (!_shellReady || _session.CurrentUserId != user || Shell.Current is null) return false;
                        var n = item.Notification;
                        var peer = n.IsGroup ? "" : $"&peerId={n.SenderId}";
                        await Shell.Current.GoToAsync($"chat?chatId={n.ChatId}&isGroup={n.IsGroup}&messageId={n.MessageId}{peer}", false);
                        return true;
                    });
                    if (!opened || !_shellReady || _session.CurrentUserId != user) return;
                    Preferences.Set(key, history);
                }
                lock (_pending) { if (_pending.Count > 0 && _pending.Peek() == item) _pending.Dequeue(); }
            }
        }
        finally { _gate.Release(); }
    }

    public async Task UnregisterAsync()
    {
        await _gate.WaitAsync();
        try
        {
            ClearPending();
            if (!await _registration.UnregisterAsync())
                throw new InvalidOperationException("Не удалось отключить push для текущей сессии.");
#if ANDROID || IOS
            await _firebase.UnregisterForPushNotificationsAsync();
#endif
            _initialized = false;
            _firebase.ClearQueues();
        }
        finally { _gate.Release(); }
    }
}
