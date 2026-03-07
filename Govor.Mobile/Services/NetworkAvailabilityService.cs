using System.Net;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Markdig.Extensions.TaskLists;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services;

public class NetworkAvailabilityService : IDisposable
{
    private readonly IEnumerable<IConnectivityChanged> _clients;
    private readonly ILogger<NetworkAvailabilityService> _logger;
    private readonly INetworkChecker _networkChecker;

    private readonly SemaphoreSlim _checkLock = new(1, 1);

    private bool? _currentState;
    private DateTime _lastNotification = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(2);

    public NetworkAvailabilityService(
        INetworkChecker networkChecker,
        IEnumerable<IConnectivityChanged> clients,
        ILogger<NetworkAvailabilityService> logger)
    {
        _networkChecker = networkChecker;
        _clients = clients;
        _logger = logger;

        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    // --- INITIAL CHECK ---
    public async Task CheckInitialConnectivityAsync()
    {
        await EvaluateConnectivityAsync(forceNotify: true);
    }

    // --- EVENT WRAPPER (без async void логики) ---
    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        _ = EvaluateConnectivityAsync();
    }

    // --- CORE LOGIC ---
    private async Task EvaluateConnectivityAsync(bool forceNotify = false)
    {
        // предотвращаем параллельные проверки
        if (!await _checkLock.WaitAsync(0))
        {
            _logger.LogDebug("Connectivity check skipped (already running)");
            return;
        }

        try
        {
            var hasInternet = await _networkChecker.CheckInternetAsync();

            _logger.LogInformation(
                "Connectivity evaluated → {State}",
                hasInternet ? "ONLINE" : "OFFLINE");

            // если состояние не изменилось — ничего не делаем
            if (!forceNotify && _currentState == hasInternet)
                return;

            _currentState = hasInternet;

            await NotifyClientsAsync(hasInternet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connectivity evaluation failed");
        }
        finally
        {
            _checkLock.Release();
        }
    }

    // --- NOTIFY ---
    private async Task NotifyClientsAsync(bool isOnline)
    {
        var now = DateTime.UtcNow;

        if (now - _lastNotification < _minInterval)
        {
            _logger.LogDebug("Notification throttled");
            return;
        }

        _lastNotification = now;

        var tasks = _clients.Select(client => SafeNotifyClientAsync(client, isOnline));

        await Task.WhenAll(tasks);
    }

    private async Task SafeNotifyClientAsync(IConnectivityChanged client, bool isOnline)
    {
        try
        {
            if (isOnline)
                await client.OnInternetConnectedAsync();
            else
                await client.OnInternetDisconnectedAsync();

            _logger.LogDebug(
                "{Client} notified → {State}",
                client.GetType().Name,
                isOnline ? "ONLINE" : "OFFLINE");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{Client} failed during connectivity notification",
                client.GetType().Name);
        }
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _checkLock.Dispose();
    }
}