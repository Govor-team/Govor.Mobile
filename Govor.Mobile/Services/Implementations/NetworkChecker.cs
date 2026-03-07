using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

public class NetworkChecker : INetworkChecker
{
    private readonly HttpClient _httpClient;
    private readonly IServerIpProvider _serverIpProvider;
    private readonly ILogger<NetworkChecker> _logger;

    public NetworkChecker(HttpClient httpClient, IServerIpProvider serverIpProvider, ILogger<NetworkChecker> logger)
    {
        _httpClient = httpClient;
        _serverIpProvider = serverIpProvider;
        _logger = logger;
    }

    public async Task<bool> CheckInternetAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_serverIpProvider.IP + "/server/ping");
            return response.IsSuccessStatusCode;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Ping - false");
            return false;
        }
    }
}