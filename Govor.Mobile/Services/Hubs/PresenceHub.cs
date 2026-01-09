using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Hubs;

public class PresenceHub : IPresenceHubService
{
    
    private readonly HubConnection _hubConnection;
    private readonly ILogger<PresenceHub> _logger;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IServerIpProvader _ipProvider;
    
    public event Action<Guid>? OnUserOnline;
    public event Action<Guid>? OnUserOffline;

    public PresenceHub(IJwtProviderService jwtProvider, IServerIpProvader ipProvader, ILogger<PresenceHub> logger)
    {
        _logger = logger;
        _ipProvider = ipProvader;
        _jwtProvider = jwtProvider;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_ipProvider.IP}/hubs/presence",
                options =>
                {
                    options.AccessTokenProvider = async () => { return await _jwtProvider.GetAccessTokenAsync(); };
                })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed: {0}", error?.Message);

            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR reconnected after token refresh.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect SignalR after token refresh.");
            }
        };
        
        _hubConnection.On("UserOnline", (Guid userId) =>
        {
            if(userId != Guid.Empty)
                OnUserOnline?.Invoke(userId);
        });

        _hubConnection.On("UserOffline", (Guid userId) =>
        {
            if(userId != Guid.Empty)
                OnUserOffline?.Invoke(userId);
        });
        
        _hubConnection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected automatically.");
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to PresenceHub.");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
            _logger.LogInformation("Disconnected from PresenceHub.");
        }
    }
}