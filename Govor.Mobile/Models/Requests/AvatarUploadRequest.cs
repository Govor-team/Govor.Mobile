namespace Govor.Mobile.Models.Requests;

public class AvatarUploadRequest
{
    public Stream FileStream { get; set; }
    public string FileName { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string MimeType { get; set; } = string.Empty;
}