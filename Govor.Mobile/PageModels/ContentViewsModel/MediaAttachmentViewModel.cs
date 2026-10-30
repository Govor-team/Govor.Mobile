using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class MediaAttachmentViewModel(Guid id, Govor.Mobile.Services.Interfaces.IMediaLoaderService media, Govor.Mobile.Services.E2ee.E2eeClient e2ee) : ObservableObject
{
    private static readonly SemaphoreSlim Downloads = new(2);
    [ObservableProperty] private ImageSource? preview;
    [ObservableProperty] private bool isVideo;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayHeight))]
    [NotifyPropertyChangedFor(nameof(HasMediaPreview))]
    private bool isDocument;
    public double DisplayHeight => IsDocument ? 88 : 174;
    public bool HasMediaPreview => !IsDocument;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string title = "Вложение";
    [ObservableProperty] private string hint = "Нажмите, чтобы открыть";
    private bool _loaded;

    public async Task LoadPreviewAsync()
    {
        if (_loaded || IsBusy) return;
        IsBusy = true;
        await Downloads.WaitAsync();
        try
        {
            var secret = await e2ee.GetAttachmentAsync(id) ?? throw new InvalidDataException();
            IsVideo = secret.MimeType is "video/mp4" or "video/quicktime";
            IsDocument = !AttachmentFiles.IsGallery(secret.MimeType);
            Title = IsDocument ? AttachmentFiles.SafeName(secret.FileName) : IsVideo ? "▶ Видео" : "Фото";
            Hint = IsDocument ? "Нажмите, чтобы сохранить или переслать" : "Нажмите, чтобы открыть";
            if (!IsVideo && !IsDocument)
            {
                var bytes = await ReadAsync();
                if (GalleryMediaPolicy.Inspect(bytes).IsVideo) throw new InvalidDataException();
                var thumbnail = await Task.Run(() => GalleryImagePreview.Create(bytes));
                Preview = ImageSource.FromStream(() => new MemoryStream(thumbnail, false));
            }
            _loaded = true;
        }
        catch { Hint = "Не удалось загрузить. Нажмите для повтора"; }
        finally { Downloads.Release(); IsBusy = false; }
    }

    private async Task<byte[]> ReadAsync()
    {
        var result = await media.Download(id, encryptedOnly: true);
        if (!result.IsSuccess) throw new InvalidDataException();
        await using var stream = result.Value.FileStream;
        var bytes = await GalleryMediaPolicy.ReadAsync(stream);
        return bytes;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task OpenAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var bytes = await ReadAsync();
            var secret = await e2ee.GetAttachmentAsync(id) ?? throw new InvalidDataException();
            if (!AttachmentFiles.IsGallery(secret.MimeType))
            {
                // Keep each attachment in its own directory; remote names cannot escape it.
                var directory = Path.Combine(FileSystem.CacheDirectory, "message-files", id.ToString("N"));
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, AttachmentFiles.SafeName(secret.FileName));
                await File.WriteAllBytesAsync(path, bytes);
                await Share.Default.RequestAsync(new ShareFileRequest { Title = secret.FileName,
                    File = new ShareFile(path, secret.MimeType) });
                return;
            }
            var format = GalleryMediaPolicy.Inspect(bytes);
            await Shell.Current.Navigation.PushModalAsync(new Govor.Mobile.Pages.MainFlow.GalleryMediaPage(bytes, format.IsVideo, format.Extension));
        }
        catch
        {
            await GlassDialogs.DisplayAlertAsync("Вложение", "Не удалось открыть медиа. Файл недоступен, повреждён или не прошёл проверку безопасности.", "OK");
        }
        finally { IsBusy = false; }
    }
}
