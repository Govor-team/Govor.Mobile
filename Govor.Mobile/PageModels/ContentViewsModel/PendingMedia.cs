namespace Govor.Mobile.PageModels.ContentViewsModel;

public sealed class PendingMedia(byte[] data, byte[]? preview, string? documentName = null)
{
    private readonly (string Mime, string Extension, bool IsVideo) _format = documentName is null
        ? GalleryMediaPolicy.Inspect(data) : (AttachmentFiles.Mime(documentName), Path.GetExtension(documentName), false);
    public bool IsDocument => documentName is not null;
    public byte[] Data { get; } = data;
    public Guid? UploadedId { get; set; }
    public string FileName { get; } = documentName is not null ? AttachmentFiles.SafeName(documentName)
        : "media-" + Guid.NewGuid().ToString("N") + GalleryMediaPolicy.Inspect(data).Extension;
    public string MimeType => _format.Mime;
    public bool IsVideo => _format.IsVideo;
    public string Label => IsDocument ? FileName : IsVideo ? "▶ Видео" : "Фото";
    public string Size => $"{Data.Length / 1_000_000d:0.0} МБ";
    public ImageSource? Preview { get; } = preview is null ? null : ImageSource.FromStream(() => new MemoryStream(preview, false));
}
