using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Hubs;

public interface IChatHub : IHubClient
{
    public event Action<UserMessageResponse>? MessageSent;
    public event Action<UserMessageResponse>? ReceiveMessage;
    
    public event Action<MessageRemovedResponse>? MessageRemoved;
    public event Action<MessageEditResponse>? MessageEdited;

    Task<HubResult<UserMessageResponse>> Send(MessageRequest request);
    Task<HubResult<MessageRemovedResponse>> Remove(RemoveMessageRequest request);
    Task<HubResult<MessageEditResponse>> Edit(EditMessageRequest request);
}