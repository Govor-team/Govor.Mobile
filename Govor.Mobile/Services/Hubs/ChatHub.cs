using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Hubs;

public class ChatHub : IChatHub
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<ChatHub> _logger;
    private readonly IJwtProviderService _jwtProvider;
    private readonly IServerIpProvader _ipProvider;
    
    public ChatHub(ILogger<ChatHub> logger, IJwtProviderService jwtProvider, IServerIpProvader ipProvider)
    {
        _logger = logger;
        _jwtProvider = jwtProvider;
        _ipProvider = ipProvider;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_ipProvider.IP}/hubs/chats",
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
        
        _hubConnection.On("ReceiveMessage", (UserMessageResponse dto) =>
        {
            if(dto.MessageId != Guid.Empty && !string.IsNullOrEmpty(dto.EncryptedContent))
                ReceiveMessage?.Invoke(dto);
            
            _logger.LogInformation("Received message: {0}", dto.MessageId);
        });
        
        _hubConnection.On("MessageSent", (UserMessageResponse dto) =>
        {
            if(dto.MessageId != Guid.Empty && !string.IsNullOrEmpty(dto.EncryptedContent))
                MessageSent?.Invoke(dto);
            
            _logger.LogInformation("Sent message: {0}", dto.MessageId);
        });

        _hubConnection.On("MessageRemoved", (MessageRemovedResponse dto) =>
        {
            if(dto.MessageId != Guid.Empty)
                MessageRemoved?.Invoke(dto);
            _logger.LogInformation("Removed message: {0}", dto.MessageId);
        });
        
        _hubConnection.On("MessageEdited", (MessageEditResponse dto) =>
        {
            if(dto.MessageId != Guid.Empty && !string.IsNullOrEmpty(dto.NewEncryptedContent))
                MessageEdited?.Invoke(dto);
            
            _logger.LogInformation("Edited message: {0}", dto.MessageId);
        });
        
        _hubConnection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected automatically.");
            return Task.CompletedTask;
        };
        #endregion
    }

    public event Action<UserMessageResponse>? MessageSent;
    public event Action<UserMessageResponse>? ReceiveMessage;
    public event Action<MessageRemovedResponse>? MessageRemoved;
    public event Action<MessageEditResponse>? MessageEdited;

    public async Task<HubResult<UserMessageResponse>> Send(MessageRequest request)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<UserMessageResponse>>("Send", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Send");
            return HubResult<UserMessageResponse>.Error("Error during sending the message");
        }
    }

    public async Task<HubResult<MessageRemovedResponse>> Remove(RemoveMessageRequest request)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<MessageRemovedResponse>>("Remove", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Remove");
            return HubResult<MessageRemovedResponse>.Error("Error during removing the message");
        }
    }

    public async Task<HubResult<MessageEditResponse>> Edit(EditMessageRequest request)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<MessageEditResponse>>("Edit", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Edit");
            return HubResult<MessageEditResponse>.Error("Error during editing the message");
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