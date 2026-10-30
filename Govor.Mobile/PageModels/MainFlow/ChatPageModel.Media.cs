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
        var source = await GlassDialogs.AttachmentSourceAsync();
        if (source is not null) await PickAttachmentsAsync(source == "files");
    }

    [RelayCommand]
    private Task AttachFilesAsync() => PickAttachmentsAsync(true);

    private async Task PickAttachmentsAsync(bool documents)
    {
        if (!CanWrite || IsEditingMessage || !CanChangeMedia) return;
        if (PendingMedia.Count >= GalleryMediaPolicy.MaxFiles)
        {
            await GlassDialogs.DisplayAlertAsync("Вложения", "Можно прикрепить до 10 файлов.", "OK");
            return;
        }
        IsPickingMedia = true;
        var chat = ChatId;
        var group = IsGroup;
        try
        {
            var selected = documents
                ? (await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Прикрепить файлы" }))?.Select(SelectedAttachment.FromFile).ToArray() ?? []
                : await GalleryPicker.PickAsync(GalleryMediaPolicy.MaxFiles - PendingMedia.Count);
            if (chat != ChatId || group != IsGroup || !CanWrite) return;
            if (selected.Count + PendingMedia.Count > GalleryMediaPolicy.MaxFiles)
                throw new InvalidDataException("Можно прикрепить до 10 файлов. Выберите меньше файлов.");
            var additions = new List<PendingMedia>();
            var total = PendingMedia.Sum(f => f.Data.Length);
            foreach (var file in selected)
            {
                await using var stream = await file.OpenReadAsync();
                var bytes = await GalleryMediaPolicy.ReadAsync(stream, Math.Min(GalleryMediaPolicy.MaxFileBytes, GalleryMediaPolicy.MaxBatchBytes - total));
                if (documents) additions.Add(new(bytes, null, file.FileName));
                else
                {
                    var format = GalleryMediaPolicy.Inspect(bytes);
                    var preview = format.IsVideo ? null : await Task.Run(() => GalleryImagePreview.Create(bytes));
                    additions.Add(new(bytes, preview));
                }
                total += bytes.Length;
            }
            if (chat == ChatId && group == IsGroup && CanWrite)
                foreach (var addition in additions) PendingMedia.Add(addition);
        }
        catch (OperationCanceledException) { }
        catch (PermissionException)
        {
            await GlassDialogs.DisplayAlertAsync("Галерея", "Разрешите доступ к выбранным фото и видео в настройках приложения.", "OK");
        }
        catch (InvalidDataException ex)
        {
            await GlassDialogs.DisplayAlertAsync("Вложения", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Attachment picker failed: {ex}");
            await GlassDialogs.DisplayAlertAsync("Вложения", "Не удалось открыть выбранные файлы. Попробуйте ещё раз.", "OK");
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
            if (file.IsDocument)
            {
                await GlassDialogs.AlertAsync(file.FileName, $"Файл · {file.Size}\nБудет отправлен с сообщением.", "Закрыть");
                return;
            }
            var format = GalleryMediaPolicy.Inspect(file.Data);
            await Shell.Current.Navigation.PushModalAsync(new Govor.Mobile.Pages.MainFlow.GalleryMediaPage(file.Data, format.IsVideo, format.Extension));
        }
        catch
        {
            await GlassDialogs.DisplayAlertAsync("Предпросмотр", "Не удалось открыть выбранное медиа.", "OK");
        }
    }
}
