using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UXDivers.Popups.Maui;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace Govor.Mobile.Pages.ContentViews;

public partial class ImagePreviewPopup : PopupPage
{
    private IImage? _currentImage;
    private Stream? _originalStream;
    public ImagePreviewPopup(ImageSource src)
    {
        InitializeComponent();
        
        PreviewImage.Source = src;
        _ = LoadImageAsync(src);
    }
    private async Task LoadImageAsync(ImageSource source)
    {
        try
        {
            if (source is FileImageSource fis && !string.IsNullOrEmpty(fis.File))
            {
                string path = Path.Combine(FileSystem.CacheDirectory, fis.File);
                if (File.Exists(path))
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    _originalStream = new MemoryStream(bytes);
                }
            }
            else if (source is StreamImageSource sis)
            {
                using var stream = await sis.Stream(CancellationToken.None);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                _originalStream = ms;
            }

            if (_originalStream != null)
            {
                _currentImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(_originalStream);
                PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_currentImage.AsBytes()));
            }
        }
        catch (Exception ex)
        {
           
        }
    }
    
    private void RotateClicked(object? sender, EventArgs e)
    {
       
    }

    private async void SaveClicked(object? sender, EventArgs e)
    {
       if (_currentImage == null)
        {
            return;
        }

        try
        {
            string fileName = $"avatar_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

#if ANDROID
            // Android — сохраняем в Pictures и добавляем в галерею
            var bytes = _currentImage.AsBytes(ImageFormat.Jpeg, quality: 85);

            string picturesPath = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures)!.AbsolutePath, fileName);

            await File.WriteAllBytesAsync(picturesPath, bytes);

            // Уведомляем галерею (MediaScanner)
            var mediaScanIntent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
            mediaScanIntent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(picturesPath)));
            Android.App.Application.Context.SendBroadcast(mediaScanIntent);
#else
            // Windows / macOS — просто в Downloads или Pictures
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Avatars");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);

            var bytes = _currentImage.AsBytes(ImageFormat.Jpeg);
            await File.WriteAllBytesAsync(path, bytes);
            
#endif
        }
        catch (Exception ex)
        {
        }
    }
}