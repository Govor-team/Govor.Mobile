
namespace Govor.Mobile.Models.Responses;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; } // or GroupId
    public RecipientType RecipientType { get; set; }
    public string EncryptedContent { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    
    public List<MediaAttachmentResponse> MediaAttachments { get; set; } = new();
    public List<MessageReactionResponse> Reactions { get; set; } = new();
    public List<MessageViewResponse> MessageViews { get; set; } = new();
}

public enum RecipientType
{
    User,
    Group
}