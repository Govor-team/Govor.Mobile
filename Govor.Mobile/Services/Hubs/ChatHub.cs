using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Reactions;
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
    private readonly IServerIpProvider _ipProvider;
    
    public ChatHub(ILogger<ChatHub> logger, IJwtProviderService jwtProvider, IServerIpProvider ipProvider, IRetryPolicy retryPolicy)
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
            .WithAutomaticReconnect(retryPolicy)
            .Build();

        _hubConnection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed.");
            return Task.CompletedTask;
        };

        _hubConnection.On<MessageReactionsChangedResponse>("MessageReactionsChanged", dto => ReactionsChanged?.Invoke(dto));
        _hubConnection.On<ChannelReactionPolicy>("ChannelReactionPolicyChanged", dto => ReactionPolicyChanged?.Invoke(dto));
        _hubConnection.On<Govor.Mobile.Models.Groups.GroupProfileChangedResponse>("GroupProfileChanged", dto =>
        {
            _logger.LogInformation("Group profile changed: {GroupId}", dto.GroupId);
            GroupProfileChanged?.Invoke(dto);
        });
        _hubConnection.On<Govor.Mobile.Models.Groups.GroupMemberChangedResponse>("GroupMemberChanged", dto =>
        {
            _logger.LogInformation("Group membership changed: {GroupId}, status {Status}", dto.GroupId, dto.Status);
            GroupMemberChanged?.Invoke(dto);
        });
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

        _hubConnection.On("MessageReaded", (MessageReadResponse dto) =>
        {
            if (dto.MessageId != Guid.Empty && dto.ReaderId != Guid.Empty && dto.ViewId != Guid.Empty)
                MessageRead?.Invoke(dto);
        });
        _hubConnection.On<ChatReadResponse>("ChatRead", dto => ChatRead?.Invoke(dto));

        _hubConnection.Reconnecting += error =>
        {
            Console.WriteLine("Connection lost...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += id =>
        {
            Reconnected?.Invoke();
            Console.WriteLine("Connection restored");
            return Task.CompletedTask;
        };
        #endregion
    }

    public event Action<UserMessageResponse>? MessageSent;
    public event Action<MessageReadResponse>? MessageRead;
    public event Action<ChatReadResponse>? ChatRead;
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

    public async Task<HubResult<MessageReadResponse>> Read(ReadMessageRequest request)
    {
        try
        {
            return await _hubConnection.InvokeAsync<HubResult<MessageReadResponse>>("Read", request);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error calling Read");
            return HubResult<MessageReadResponse>.Error("Error during rading the message");
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
    
    public event Action<MessageReactionsChangedResponse>? ReactionsChanged;
    public event Action<ChannelReactionPolicy>? ReactionPolicyChanged;
    public event Action<Govor.Mobile.Models.Groups.GroupProfileChangedResponse>? GroupProfileChanged;
    public event Action<Govor.Mobile.Models.Groups.GroupMemberChangedResponse>? GroupMemberChanged;
    public event Action? Reconnected;
    public Task<HubResult<MessageReactionsChangedResponse>> React(Guid messageId, Guid reactionId) =>
        _hubConnection.InvokeAsync<HubResult<MessageReactionsChangedResponse>>("React", messageId, new { reactionId });
    public Task<HubResult<MessageReactionsChangedResponse>> RemoveReaction(Guid messageId) =>
        _hubConnection.InvokeAsync<HubResult<MessageReactionsChangedResponse>>("RemoveReaction", messageId);

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
