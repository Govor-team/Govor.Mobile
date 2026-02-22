using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Hubs;

public class HubInitializer : IHubInitializer, IConnectivityChanged
{
    private readonly IEnumerable<IHubClient> _clients;
    private readonly ILogger<HubInitializer> _logger;

    public HubInitializer(IEnumerable<IHubClient> clients, ILogger<HubInitializer> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task ConnectAllAsync()
    {
        foreach (var hub in _clients)
        {
            try
            {
                await hub.ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to hub {0}", hub.GetType().Name);
            }
        }
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var hub in _clients)
        {
            try
            {
                await hub.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect hub {0}", hub.GetType().Name);
            }
        }
    }

    public Task OnInternetConnectedAsync() => ConnectAllAsync();

    public Task OnInternetDisconnectedAsync() => DisconnectAllAsync();
}