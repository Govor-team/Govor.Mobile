namespace Govor.Mobile.Models.Responses;

public class MessageEditResponse
{
    public Guid MessageId { get; set; }
    public Guid EditorId { get; set; }
    public Guid RecipientId { get; init; }
    public RecipientType RecipientType{get; init; }
    public string NewEncryptedContent { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; }
}