using System.Reflection;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Hubs;

public class ProfileHub : IProfileHubService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<ProfileHub> _logger;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IServerIpProvader _ipProvider;
    
    public event Action<Guid, DescriptionUpdatePayload>? OnDescriptionUpdated;
    public event Action<Guid, AvatarUpdatePayload>? OnAvatarUpdated;

    public ProfileHub(IJwtProviderService jwtProvider, IServerIpProvader ipProvader,  ILogger<ProfileHub> logger)
    {
        _logger = logger;
        _ipProvider = ipProvader;
        _jwtProvider = jwtProvider; 
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_ipProvider.IP}/hubs/profiles", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    return await _jwtProvider.GetAccessTokenAsync();
                };
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
        
        _hubConnection.On("DescriptionUpdated", (DescriptionUpdatePayload payload) =>
        {
            if (payload is not null)
            {
                var userId = payload.UserId;
                
                OnDescriptionUpdated?.Invoke(userId, payload);
            }
        });

        _hubConnection.On("AvatarUpdated", (AvatarUpdatePayload payload) =>
        {
            if (payload is not null)
            {
                var iconId = payload.IconId;
                OnAvatarUpdated?.Invoke(iconId, payload);
            }
        });
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to ProfileHub.");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
            _logger.LogInformation("Disconnected from ProfileHub.");
        }
    }

    public async Task<HubResult<bool>> SetDescriptionAsync(string description)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<bool>>("SetDescription", description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SetDescriptionAsync");
            return HubResult<bool>.Error("Error updating the description");
        }
    }

    public async Task<HubResult<bool>> SetAvatarAsync(Guid iconId)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<bool>>("SetAvatar", iconId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SetAvatarAsync");
            return HubResult<bool>.Error("Error updating the avatar");
        }
    }
}