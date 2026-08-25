using Govor.Mobile.Models.Requests;

namespace Govor.Mobile.Models.Responses;

public class MessageRemovedResponse
{
    public required Guid MessageId { get; set; }
    public required Guid SenderId { get; set; }
    public required Guid RecipientId { get; set; }
    public RemoveMessageRequestType RequestType { get; set; }
    public RecipientType RecipientType { get; set; }
}