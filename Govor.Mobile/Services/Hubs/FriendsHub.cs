using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Hubs;

public class FriendsHub : IFriendsHubService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<PresenceHub> _logger;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IServerIpProvader _ipProvider;

    public event Action<FriendshipDto>? YourFriendRequestReceived;
    public event Action<FriendshipDto>? FriendRequestReceived;
    
    public event Action<FriendshipDto>? YourFriendRequestAccepted;
    public event Action<FriendshipDto>? FriendRequestAccepted;
    
    public event Action<FriendshipDto>? YourFriendRequestRejected;
    public event Action<FriendshipDto>? FriendRequestRejected;

    public FriendsHub(ILogger<PresenceHub> logger, IJwtProviderService jwtProvider, IServerIpProvader ipProvider)
    {
        _logger = logger;
        _jwtProvider = jwtProvider;
        _ipProvider = ipProvider;
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_ipProvider.IP}/hubs/friends",
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

        #region Events
        
        _hubConnection.On("YourFriendRequestReceived", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                YourFriendRequestReceived?.Invoke(dto);
        });
        
        _hubConnection.On("FriendRequestReceived", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                FriendRequestReceived?.Invoke(dto);
        });

        _hubConnection.On("YourFriendRequestAccepted", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                YourFriendRequestAccepted?.Invoke(dto);
        });
        
        _hubConnection.On("FriendRequestAccepted", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                FriendRequestAccepted?.Invoke(dto);
        });

        
        _hubConnection.On("YourFriendRequestRejected", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                YourFriendRequestRejected?.Invoke(dto);
        });

        _hubConnection.On("FriendRequestRejected", (FriendshipDto dto) =>
        {
            if(dto.Id != Guid.Empty)
                FriendRequestRejected?.Invoke(dto);
        });
        
        _hubConnection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected automatically.");
            return Task.CompletedTask;
        };
        #endregion
    }
    
    public async Task<HubResult<object>> SendRequestAsync(Guid targetUserId)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<object>>("SendRequest", targetUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SetDescriptionAsync");
            return HubResult<object>.Error("Error during sending the request");
        }
    }

    public async Task<HubResult<object>> AcceptRequestAsync(Guid friendshipId)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<object>>("AcceptRequest", friendshipId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AcceptRequestAsync");
            return HubResult<object>.Error("Error during accepting the request");
        }
    }

    public async Task<HubResult<object>> RejectRequestAsync(Guid friendshipId)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<object>>("RejectRequest", friendshipId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling RejectRequestAsync");
            return HubResult<object>.Error("Error during rejecting the request");
        }
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

}