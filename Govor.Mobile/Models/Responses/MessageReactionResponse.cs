namespace Govor.Mobile.Models.Responses;

public class MessageReactionResponse
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string ReactionCode { get; set; } // "❤️", "🔥", "👍", ":custom_emoji:" 
    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;
}