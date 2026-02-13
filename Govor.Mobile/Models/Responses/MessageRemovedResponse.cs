namespace Govor.Mobile.Models.Responses;

public class MessageRemovedResponse
{
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public RecipientType RecipientType { get; set; }
}
