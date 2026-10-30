using Android.App;
using Android.Content;
using Govor.Mobile.Services;

namespace Govor.Mobile.Platforms.Android;

/// <summary>One system gallery for photos AND videos, without asking for library access.</summary>
internal static class MixedGalleryPicker
{
    private const int RequestCode = 4816;
    private static TaskCompletionSource<IReadOnlyList<SelectedAttachment>>? _pending;

    public static async Task<IReadOnlyList<SelectedAttachment>> PickAsync(int limit)
    {
        if (_pending is not null) return [];
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Галерея недоступна.");
        var completion = new TaskCompletionSource<IReadOnlyList<SelectedAttachment>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = completion;
        try
        {
            Intent intent;
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                intent = new Intent("android.provider.action.PICK_IMAGES");
                // Without a MIME filter the photo picker shows both images and videos.
                if (limit > 1) intent.PutExtra("android.provider.extra.PICK_IMAGES_MAX", limit);
            }
            else
            {
                intent = new Intent(Intent.ActionGetContent);
                intent.SetType("*/*");
                intent.PutExtra(Intent.ExtraMimeTypes, new[] { "image/*", "video/*" });
                intent.PutExtra(Intent.ExtraAllowMultiple, limit > 1);
                intent.AddCategory(Intent.CategoryOpenable);
            }
            try { activity.StartActivityForResult(intent, RequestCode); }
            catch (ActivityNotFoundException)
            {
                intent = new Intent(Intent.ActionGetContent);
                intent.SetType("*/*");
                intent.PutExtra(Intent.ExtraMimeTypes, new[] { "image/*", "video/*" });
                intent.PutExtra(Intent.ExtraAllowMultiple, limit > 1);
                intent.AddCategory(Intent.CategoryOpenable);
                activity.StartActivityForResult(intent, RequestCode);
            }
            return await completion.Task;
        }
        finally { _pending = null; }
    }

    public static bool Complete(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != RequestCode) return false;
        try
        {
            var files = new List<SelectedAttachment>();
            if (resultCode == Result.Ok && data is not null)
            {
                var uris = new List<global::Android.Net.Uri>();
                if (data.ClipData is { } clips)
                    for (var i = 0; i < clips.ItemCount; i++)
                        if (clips.GetItemAt(i)?.Uri is { } uri) uris.Add(uri);
                if (uris.Count == 0 && data.Data is { } single) uris.Add(single);
                var resolver = Platform.AppContext.ContentResolver!;
                foreach (var uri in uris.DistinctBy(u => u.ToString()))
                    files.Add(new SelectedAttachment("Медиа", () => Task.FromResult<Stream>(
                        resolver.OpenInputStream(uri) ?? throw new IOException("Нет доступа к выбранному медиа."))));
            }
            _pending?.TrySetResult(files);
        }
        catch (Exception ex) { _pending?.TrySetException(ex); }
        return true;
    }
}
