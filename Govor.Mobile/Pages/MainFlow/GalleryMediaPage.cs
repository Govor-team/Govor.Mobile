using CommunityToolkit.Maui.Views;

namespace Govor.Mobile.Pages.MainFlow;

public sealed class GalleryMediaPage : ContentPage
{
    private string? _videoPath;
    private MediaElement? _player;

    public GalleryMediaPage(byte[] bytes, bool video, string extension)
    {
        BackgroundColor = Color.FromArgb("#14111D");
        var layout = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Padding = new Thickness(16, 24, 16, 16) };
        var close = new Button { Text = "✕  Закрыть", HorizontalOptions = LayoutOptions.End, BackgroundColor = Color.FromArgb("#352D49"), TextColor = Colors.White, CornerRadius = 20 };
        close.Clicked += async (_, _) => await Navigation.PopModalAsync();
        layout.Add(close);
        if (video)
        {
            var directory = Path.Combine(FileSystem.CacheDirectory, "message-video-preview");
            Directory.CreateDirectory(directory);
            _videoPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + extension);
            File.WriteAllBytes(_videoPath, bytes);
            _player = new MediaElement { Source = MediaSource.FromFile(_videoPath), ShouldAutoPlay = false, ShouldShowPlaybackControls = true, Aspect = Aspect.AspectFit };
            layout.Add(_player, 0, 1);
        }
        else
        {
            var preview = GalleryImagePreview.Create(bytes, 1600);
            layout.Add(new Image { Source = ImageSource.FromStream(() => new MemoryStream(preview, false)), Aspect = Aspect.AspectFit }, 0, 1);
        }
        Content = layout;
    }

    protected override void OnDisappearing()
    {
        if (_player is not null) { _player.Stop(); _player.Source = null; _player.Handler?.DisconnectHandler(); }
        if (_videoPath is not null)
            try { File.Delete(_videoPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        Content = null;
        base.OnDisappearing();
    }

    public static void CleanStalePreviews()
    {
        var directory = Path.Combine(FileSystem.CacheDirectory, "message-video-preview");
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.EnumerateFiles(directory))
            try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
