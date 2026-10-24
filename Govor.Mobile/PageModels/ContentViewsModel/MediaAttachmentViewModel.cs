using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class MediaAttachmentViewModel(Guid id, Govor.Mobile.Services.Interfaces.IMediaLoaderService media, Govor.Mobile.Services.E2ee.E2eeClient e2ee) : ObservableObject
{
    private static readonly SemaphoreSlim Downloads = new(2);
    [ObservableProperty] private ImageSource? preview;
    [ObservableProperty] private bool isVideo;
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
            Title = IsVideo ? "▶ Видео" : "Фото";
            if (!IsVideo)
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
        GalleryMediaPolicy.Inspect(bytes);
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
            var format = GalleryMediaPolicy.Inspect(bytes);
            await Shell.Current.Navigation.PushModalAsync(new Govor.Mobile.Pages.MainFlow.GalleryMediaPage(bytes, format.IsVideo, format.Extension));
        }
        catch
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Вложение", "Не удалось открыть медиа. Файл недоступен, повреждён или не прошёл проверку безопасности.", "OK");
        }
        finally { IsBusy = false; }
    }
}
