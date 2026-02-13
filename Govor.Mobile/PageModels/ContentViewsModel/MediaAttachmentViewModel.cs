using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class MediaAttachmentViewModel : ObservableObject
{
    [ObservableProperty]
    private string previewUrl; // Ссылка на картинку или превью видео

    [ObservableProperty]
    private string fullUrl; // Ссылка на оригинал

    [ObservableProperty]
    private bool isVideo; // Флаг для отображения иконки "Play"
}