namespace Govor.Mobile.Models.Requests;

public class EditMessageRequest
{
    public Guid MessageId { get; set; }
    public string NewEncryptedContent { get; set; } = string.Empty;
}