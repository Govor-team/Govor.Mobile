using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Pages.ContentViews;

public sealed class MessageMediaView : ContentView
{
    public MessageMediaView()
    {
        var image = new Image { Aspect = Aspect.AspectFill, HeightRequest = 174 };
        image.SetBinding(Image.SourceProperty, nameof(MediaAttachmentViewModel.Preview));
        image.SetBinding(IsVisibleProperty, nameof(MediaAttachmentViewModel.HasMediaPreview));
        var title = new Label { TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 15,
            MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation };
        title.SetBinding(Label.TextProperty, nameof(MediaAttachmentViewModel.Title));
        var hint = new Label { TextColor = Color.FromArgb("#CDC2E1"), FontSize = 11, MaxLines = 2 };
        hint.SetBinding(Label.TextProperty, nameof(MediaAttachmentViewModel.Hint));
        var spinner = new ActivityIndicator { Color = Color.FromArgb("#D9C7FF"), HorizontalOptions = LayoutOptions.End };
        spinner.SetBinding(ActivityIndicator.IsRunningProperty, nameof(MediaAttachmentViewModel.IsBusy));
        var overlay = new Grid { Padding = 12, VerticalOptions = LayoutOptions.End, BackgroundColor = Color.FromArgb("#BB211B31") };
        overlay.Add(new VerticalStackLayout { Spacing = 3, Children = { title, hint } });
        overlay.Add(spinner);
        var grid = new Grid { WidthRequest = 254, HeightRequest = 174, BackgroundColor = Color.FromArgb("#352D49") };
        grid.SetBinding(HeightRequestProperty, nameof(MediaAttachmentViewModel.DisplayHeight));
        grid.Add(image); grid.Add(overlay);
        var border = new Border { StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }, Content = grid };
        var tap = new TapGestureRecognizer();
        tap.SetBinding(TapGestureRecognizer.CommandProperty, nameof(MediaAttachmentViewModel.OpenCommand));
        border.GestureRecognizers.Add(tap);
        Content = border;
        Loaded += async (_, _) => { if (BindingContext is MediaAttachmentViewModel model) await model.LoadPreviewAsync(); };
    }
}
