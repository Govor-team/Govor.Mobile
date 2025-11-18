namespace Govor.Mobile.Application.Models.Responses;

public class DownloadMediaResponse
{
    public string FileName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public Stream FileStream { get; set; } = default!;
}
