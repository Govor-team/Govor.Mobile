using System.ComponentModel.DataAnnotations;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Models.Requests;

public record MessageRequest
{
    public Guid RecipientId { get; init; }
    public RecipientType RecipientType { get; init; }
    [Required]
    [MaxLength(100_000, ErrorMessage = "EncryptedContent cannot exceed 100,000 characters.")]
    public string EncryptedContent { get; init; } = string.Empty;
    public Guid? ReplyToMessageId { get; set; }
    public List<MediaReference> MediaAttachments { get; set; } = new();
}