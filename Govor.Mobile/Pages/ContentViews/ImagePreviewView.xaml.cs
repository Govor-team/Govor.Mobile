using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;

namespace Govor.Mobile.ContentViews;

public partial class ImagePreviewView : Popup
{
    private Stream _originalStream;
    private int _rotation = 0;

    double _currentScale = 1;
    double _startScale = 1;
    double _xOffset = 0;
    double _yOffset = 0;
    
    public ImagePreviewView(ImageSource src)
    {
        InitializeComponent();

        PreviewImage.Source = src;
    }
    
    
    // Открытие предпросмотра
    public async Task ShowAsync(ImageSource src)
    {
        _rotation = 0;

        if (src is FileImageSource fis)
        {
            _originalStream = File.OpenRead(fis.File);
        }
        else if (src is StreamImageSource sis)
        {
            _originalStream = await sis.Stream(CancellationToken.None);
        }
        else
        {
            return;
        }

        PreviewImage.Source = src;
    }
    
    private void RotateClicked(object sender, EventArgs e)
    {
        _rotation = (_rotation + 90) % 360;
        PreviewImage.Rotation = _rotation;
    }

    private async void SaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (_originalStream == null)
            {
                var streamImageSource = PreviewImage.Source as StreamImageSource;

                if (streamImageSource != null)
                {
                    var cancellationToken = CancellationToken.None;
                    Stream stream = await streamImageSource.Stream(cancellationToken);
                
                    _originalStream = stream;
                }
            }
            
            _originalStream?.Position = 0;
            
#if ANDROID
            await SaveToAndroidGallery(_originalStream);
#elif WINDOWS
            await SaveToWindowsFile(_originalStream);
#endif

            await Application.Current.MainPage.DisplayAlert("Готово", "Изображение сохранено", "Ок");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка", ex.Message, "Ок");
        }
    }

#if ANDROID
    private async Task SaveToAndroidGallery(Stream stream)
    {
        stream.Position = 0;

        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

        var values = new Android.Content.ContentValues();
        values.Put(Android.Provider.MediaStore.MediaColumns.DisplayName, fileName);
        values.Put(Android.Provider.MediaStore.MediaColumns.MimeType, "image/jpeg");
        values.Put(Android.Provider.MediaStore.MediaColumns.RelativePath, "DCIM/Govor");

        var uri = Android.App.Application.Context.ContentResolver.Insert(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri, values);

        using var output = Android.App.Application.Context.ContentResolver.OpenOutputStream(uri);
        await stream.CopyToAsync(output);
    }
#endif

#if WINDOWS
    private async Task SaveToWindowsFile(Stream stream)
    {
        var fileName = $"image_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

        var file = await Windows.Storage.KnownFolders.PicturesLibrary.CreateFileAsync(
            fileName, Windows.Storage.CreationCollisionOption.GenerateUniqueName);

        using var output = await file.OpenStreamForWriteAsync();
        stream.Position = 0;
        await stream.CopyToAsync(output);
    }
#endif
}