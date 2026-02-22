using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Markdig.Extensions.TaskLists;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services;

public class NetworkAvailabilityService : IDisposable
{
    private readonly IHubInitializer _hubInitializer;
    private readonly IEnumerable<IConnectivityChanged> _clients;
    private readonly ILogger<NetworkAvailabilityService> _logger;
    private readonly IServerIpProvider _serverIpProvider;
    
    private DateTime _lastNotification = DateTime.MinValue;
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(3);

    public NetworkAvailabilityService(
        IServerIpProvider serverIpProvider,
        IEnumerable<IConnectivityChanged> clients, 
        ILogger<NetworkAvailabilityService> logger)
    {
        _clients = clients;
        _logger = logger;
        _serverIpProvider = serverIpProvider;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }
    
    public async Task CheckInitialConnectivity()
    {
        var hasInternet = await HasRealInternetConnectionAsync();

        await NotifyClientsAsync(hasInternet);
    }

    private async Task NotifyClientsAsync(bool isOnline)
    {
        var now = DateTime.UtcNow;
        if (now - _lastNotification < _minInterval)
        {
            _logger?.LogDebug("Skipping notification — too soon after last one");
            return;
        }

        _lastNotification = now;
        
        var methodName = isOnline ? nameof(IConnectivityChanged.OnInternetConnectedAsync) 
            : nameof(IConnectivityChanged.OnInternetDisconnectedAsync);

        var tasks = _clients.Select(async client =>
        {
            try
            {
                if (isOnline)
                    await client.OnInternetConnectedAsync();
                else
                    await client.OnInternetDisconnectedAsync();

                _logger?.LogDebug("{Client} → Internet {Status}", client.GetType().Name, methodName);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{Client} failed on {Method}", 
                    client.GetType().Name, methodName);
            }
        });

        await Task.WhenAll(tasks);
    }
    
    private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        try
        {
            var hasInternet = await HasRealInternetConnectionAsync();

            _logger?.LogInformation("Network changed → HasInternet: {HasInternet}, Profiles: {Profiles}",
                hasInternet, string.Join(", ", e.ConnectionProfiles));

            await NotifyClientsAsync(hasInternet);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in network change handler");
        }
    }
    
    private async Task<bool> HasRealInternetConnectionAsync()
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            
            var response = await client.GetAsync(_serverIpProvider.IP+"/server/ping");

            // 200 → успех
            return response.IsSuccessStatusCode && (int)response.StatusCode == 200;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Internet check failed: {ex.Message}");
            return false;
        }
    }
    
    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}