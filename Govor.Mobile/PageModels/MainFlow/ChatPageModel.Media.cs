using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class ChatPageModel
{
    private readonly IMediaLoaderService _mediaLoader;
    public ObservableCollection<PendingMedia> PendingMedia { get; } = new();
    public bool HasPendingMedia => PendingMedia.Count > 0;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeMedia))]
    private bool isPickingMedia;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeMedia))]
    private bool isSendingMedia;
    [ObservableProperty] private string mediaStatus = string.Empty;
    public bool CanChangeMedia => !IsPickingMedia && !IsSendingMedia;

    [RelayCommand]
    private async Task AttachMediaAsync()
    {
        if (!CanWrite || IsEditingMessage || !CanChangeMedia) return;
        if (PendingMedia.Count >= GalleryMediaPolicy.MaxFiles)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Вложения", "Можно прикрепить до 10 файлов.", "OK");
            return;
        }
        IsPickingMedia = true;
        var chat = ChatId;
        var group = IsGroup;
        try
        {
            var choice = await Shell.Current.CurrentPage.DisplayActionSheetAsync("Прикрепить из галереи", "Отмена", null, "Фото", "Видео");
            if (choice is not ("Фото" or "Видео")) return;
            var options = new MediaPickerOptions
            {
                Title = "Выберите медиа", SelectionLimit = GalleryMediaPolicy.MaxFiles - PendingMedia.Count,
                MaximumWidth = 4096, MaximumHeight = 4096, CompressionQuality = 90,
                RotateImage = true, PreserveMetaData = false
            };
            var selected = choice == "Фото"
                ? await MediaPicker.Default.PickPhotosAsync(options)
                : await MediaPicker.Default.PickVideosAsync(options);
            if (chat != ChatId || group != IsGroup || !CanWrite) return;
            if (selected.Count + PendingMedia.Count > GalleryMediaPolicy.MaxFiles)
                throw new InvalidDataException("Можно прикрепить до 10 файлов. Выберите меньше файлов.");
            var additions = new List<PendingMedia>();
            var total = PendingMedia.Sum(f => f.Data.Length);
            foreach (var file in selected)
            {
                await using var stream = await file.OpenReadAsync();
                var bytes = await GalleryMediaPolicy.ReadAsync(stream, Math.Min(GalleryMediaPolicy.MaxFileBytes, GalleryMediaPolicy.MaxBatchBytes - total));
                var format = GalleryMediaPolicy.Inspect(bytes);
                if (format.IsVideo != (choice == "Видео")) throw new InvalidDataException("Тип выбранного файла не соответствует содержимому.");
                var preview = format.IsVideo ? null : await Task.Run(() => GalleryImagePreview.Create(bytes));
                additions.Add(new(bytes, preview));
                total += bytes.Length;
            }
            if (chat == ChatId && group == IsGroup && CanWrite)
                foreach (var addition in additions) PendingMedia.Add(addition);
        }
        catch (OperationCanceledException) { }
        catch (PermissionException)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Галерея", "Разрешите доступ к выбранным фото и видео в настройках приложения.", "OK");
        }
        catch (InvalidDataException ex)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Вложения", ex.Message, "OK");
        }
        catch (Exception)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Галерея", "Не удалось открыть медиа. Попробуйте выбрать другой файл.", "OK");
        }
        finally { IsPickingMedia = false; }
    }

    [RelayCommand]
    private void RemovePendingMedia(PendingMedia media)
    {
        if (CanChangeMedia) PendingMedia.Remove(media);
    }

    private void ClearPendingMedia() => PendingMedia.Clear();

    [RelayCommand]
    private async Task PreviewPendingMediaAsync(PendingMedia file)
    {
        if (!CanChangeMedia) return;
        try
        {
            var format = GalleryMediaPolicy.Inspect(file.Data);
            await Shell.Current.Navigation.PushModalAsync(new Govor.Mobile.Pages.MainFlow.GalleryMediaPage(file.Data, format.IsVideo, format.Extension));
        }
        catch
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Предпросмотр", "Не удалось открыть выбранное медиа.", "OK");
        }
    }
}
