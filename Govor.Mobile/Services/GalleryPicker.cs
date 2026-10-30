using FileResult = Microsoft.Maui.Storage.FileResult;

namespace Govor.Mobile.Services;

public static class GalleryPicker
{
    public static async Task<IReadOnlyList<SelectedAttachment>> PickAsync(int limit)
    {
#if ANDROID
        return await Platforms.Android.MixedGalleryPicker.PickAsync(limit);
#else
        var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Фото и видео",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.iOS] = new[] { "public.image", "public.movie" },
                [DevicePlatform.MacCatalyst] = new[] { "public.image", "public.movie" },
                [DevicePlatform.WinUI] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov" }
            })
        });
        return files?.Select(SelectedAttachment.FromFile).ToArray() ?? [];
#endif
    }
}
