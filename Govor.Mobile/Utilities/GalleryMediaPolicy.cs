namespace Govor.Mobile.Utilities;

/// <summary>Limits apply to bytes read, never to picker metadata or filename extensions.</summary>
public static class GalleryMediaPolicy
{
    public const int MaxFiles = 10;
    public const int MaxFileBytes = 19_000_000;
    public const int MaxBatchBytes = 38_000_000;

    public static async Task<byte[]> ReadAsync(Stream stream, int maximum = MaxFileBytes)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(buffer)) > 0)
        {
            if (output.Length + count > maximum)
                throw new InvalidDataException("Файл слишком большой. До 19 МБ на файл и 38 МБ на сообщение.");
            output.Write(buffer, 0, count);
        }
        if (output.Length == 0) throw new InvalidDataException("Выбран пустой файл.");
        return output.ToArray();
    }

    public static (string Mime, string Extension, bool IsVideo) Inspect(byte[] data)
    {
        var b = data.AsSpan();
        if (b.Length < 12 || b.Length > MaxFileBytes) throw Unsupported();
        if (b.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ("image/jpeg", ".jpg", false);
        if (b.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", ".png", false);
        if (b.StartsWith("GIF87a"u8) || b.StartsWith("GIF89a"u8)) return ("image/gif", ".gif", false);
        if (b[..4].SequenceEqual("RIFF"u8) && b.Slice(8, 4).SequenceEqual("WEBP"u8)) return ("image/webp", ".webp", false);
        if (b.Length >= 24 && b.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            var brand = System.Text.Encoding.ASCII.GetString(b.Slice(8, 4));
            if (brand == "qt  ") return ("video/quicktime", ".mov", true);
            if (brand is "isom" or "iso2" or "mp41" or "mp42" or "avc1" or "M4V ")
                return ("video/mp4", ".mp4", true);
        }
        throw Unsupported();
    }

    private static InvalidDataException Unsupported() => new("Поддерживаются фото JPEG, PNG, GIF, WebP и видео MP4, MOV. Содержимое файла должно соответствовать формату.");
}
