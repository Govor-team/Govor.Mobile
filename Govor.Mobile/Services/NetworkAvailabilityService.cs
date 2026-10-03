using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services;

public sealed class NetworkAvailabilityService : IDisposable
{
    private readonly IEnumerable<IConnectivityChanged> _clients;
    private readonly ILogger<NetworkAvailabilityService> _logger;
    private readonly INetworkChecker _checker;
    private readonly IAuthService _auth;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool? _currentState;
    private bool _enabled;

    public NetworkAvailabilityService(INetworkChecker networkChecker,
        IEnumerable<IConnectivityChanged> clients, ILogger<NetworkAvailabilityService> logger,
        IAuthService auth)
    {
        _checker = networkChecker;
        _clients = clients;
        _logger = logger;
        _auth = auth;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public Task CheckInitialConnectivityAsync()
    {
        _enabled = true;
        return EvaluateAsync(true);
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (_enabled) _ = EvaluateAsync(false);
    }

    private async Task EvaluateAsync(bool force)
    {
        // Queue state changes instead of dropping a disconnect while a check is in progress.
        await _gate.WaitAsync();
        try
        {
            var online = _auth.IsAuthenticated && await _checker.CheckInternetAsync();
            if (!force && _currentState == online) return;
            _currentState = online;
            await Task.WhenAll(_clients.Select(client => NotifyAsync(client, online)));
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Connectivity evaluation failed."); }
        finally { _gate.Release(); }
    }

    private async Task NotifyAsync(IConnectivityChanged client, bool online)
    {
        try
        {
            if (online && _auth.IsAuthenticated) await client.OnInternetConnectedAsync();
            else await client.OnInternetDisconnectedAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Connectivity client {Client} failed.", client.GetType().Name); }
    }

    public void Dispose() => Connectivity.ConnectivityChanged -= OnConnectivityChanged;
}
