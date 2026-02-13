namespace Govor.Mobile.Models.Responses;

public class MessageViewResponse
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ViewedAt { get; set; }
}