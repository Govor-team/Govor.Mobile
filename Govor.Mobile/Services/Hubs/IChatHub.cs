using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Hubs;

public interface IChatHub : IHubClient
{
    public event Action<UserMessageResponse>? MessageSent;
    public event Action<UserMessageResponse>? ReceiveMessage;
    public event Action<MessageReadResponse>? MessageRead;
    event Action<ChatReadResponse>? ChatRead;
    public event Action<MessageRemovedResponse>? MessageRemoved;
    public event Action<MessageEditResponse>? MessageEdited;

    event Action<Govor.Mobile.Models.Reactions.MessageReactionsChangedResponse>? ReactionsChanged;
    event Action<Govor.Mobile.Models.Reactions.ChannelReactionPolicy>? ReactionPolicyChanged;
    event Action<Govor.Mobile.Models.Groups.GroupProfileChangedResponse>? GroupProfileChanged;
    event Action<Govor.Mobile.Models.Groups.GroupMemberChangedResponse>? GroupMemberChanged;
    event Action? Reconnected;
    Task<HubResult<Govor.Mobile.Models.Reactions.MessageReactionsChangedResponse>> React(Guid messageId, Guid reactionId);
    Task<HubResult<Govor.Mobile.Models.Reactions.MessageReactionsChangedResponse>> RemoveReaction(Guid messageId);

    Task<HubResult<UserMessageResponse>> Send(MessageRequest request);
    Task<HubResult<MessageReadResponse>> Read(ReadMessageRequest request);
    Task<HubResult<MessageRemovedResponse>> Remove(RemoveMessageRequest request);
    Task<HubResult<MessageEditResponse>> Edit(EditMessageRequest request);
}
