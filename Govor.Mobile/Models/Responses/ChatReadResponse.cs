namespace Govor.Mobile.Models.Responses;

public sealed class ChatReadResponse
{
    public Guid ChatId { get; set; }
    public RecipientType RecipientType { get; set; }
    public Guid ReaderId { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class UnreadCountResponse
{
    public int UnreadCount { get; set; }
}
