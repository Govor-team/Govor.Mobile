namespace Govor.Mobile.Utilities;

public static class AttachmentFiles
{
    public static bool IsGallery(string mime) => mime is "image/jpeg" or "image/png" or "image/gif" or "image/webp"
        or "video/mp4" or "video/quicktime";

    public static string SafeName(string name)
    {
        name = Path.GetFileName(name.Replace('\\', '/'));
        var safe = new string(name.Where(c => !char.IsControl(c) && !"<>:\"/\\|?*".Contains(c)).ToArray()).Trim().Trim('.');
        return string.IsNullOrWhiteSpace(safe) ? "Вложение" : safe;
    }

    public static string Mime(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".txt" => "text/plain",
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".doc" => "application/msword",
        _ => "application/octet-stream"
    };
}
