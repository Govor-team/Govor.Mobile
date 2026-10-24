using SkiaSharp;

namespace Govor.Mobile.Utilities;

public static class GalleryImagePreview
{
    public static byte[] Create(byte[] bytes, int side = 640)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Не удалось прочитать изображение.");
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 16384 || info.Height > 16384 ||
            (long)info.Width * info.Height > 40_000_000)
            throw new InvalidDataException("Изображение слишком большое: максимум 40 мегапикселей.");
        var scale = Math.Min(1f, (float)side / Math.Max(info.Width, info.Height));
        var size = codec.GetScaledDimensions(scale);
        using var bitmap = SKBitmap.Decode(codec, new SKImageInfo(size.Width, size.Height))
            ?? throw new InvalidDataException("Повреждённое изображение.");
        using var resized = bitmap.Resize(new SKImageInfo(Math.Max(1, (int)(info.Width * scale)),
            Math.Max(1, (int)(info.Height * scale))), SKSamplingOptions.Default);
        using var image = SKImage.FromBitmap(resized ?? bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }
}
