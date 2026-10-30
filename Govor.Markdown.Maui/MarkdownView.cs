using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Govor.Markdown.Maui;

public sealed class LinkEventArgs(string url) : EventArgs
{
    public string Url { get; } = url;
    public bool Handled { get; set; }
}

/// <summary>Native, non-scrolling renderer. One Label for an ordinary message; no WebView.</summary>
public sealed class MarkdownView : ContentView
{
    private static readonly SemaphoreSlim ParseSlots = new(2);
    private int _revision;
    private CancellationTokenSource? _pending;
    private MarkdownDocument? _document;

    public static readonly BindableProperty MarkdownTextProperty = BindableProperty.Create(
        nameof(MarkdownText), typeof(string), typeof(MarkdownView), "", propertyChanged: TextChanged);
    public string MarkdownText { get => (string)GetValue(MarkdownTextProperty); set => SetValue(MarkdownTextProperty, value); }

    public static readonly BindableProperty TextColorProperty = CreateStyleProperty(nameof(TextColor), Colors.Black);
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public static readonly BindableProperty HyperlinkColorProperty = CreateStyleProperty(nameof(HyperlinkColor), Colors.CornflowerBlue);
    public Color HyperlinkColor { get => (Color)GetValue(HyperlinkColorProperty); set => SetValue(HyperlinkColorProperty, value); }
    public static readonly BindableProperty HeadingColorProperty = CreateStyleProperty(nameof(HeadingColor), Colors.MediumPurple);
    public Color HeadingColor { get => (Color)GetValue(HeadingColorProperty); set => SetValue(HeadingColorProperty, value); }
    public static readonly BindableProperty CodeBackgroundColorProperty = CreateStyleProperty(nameof(CodeBackgroundColor), Colors.LightGray);
    public Color CodeBackgroundColor { get => (Color)GetValue(CodeBackgroundColorProperty); set => SetValue(CodeBackgroundColorProperty, value); }
    public static readonly BindableProperty HighlightColorProperty = CreateStyleProperty(nameof(HighlightColor), Colors.Goldenrod);
    public Color HighlightColor { get => (Color)GetValue(HighlightColorProperty); set => SetValue(HighlightColorProperty, value); }
    public static readonly BindableProperty FontSizeProperty = CreateStyleProperty(nameof(FontSize), 14d);
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public static readonly BindableProperty ImageHeightProperty = CreateStyleProperty(nameof(ImageHeight), 160d);
    public double ImageHeight { get => (double)GetValue(ImageHeightProperty); set => SetValue(ImageHeightProperty, value); }
    public static readonly BindableProperty BlockSpacingProperty = CreateStyleProperty(nameof(BlockSpacing), 6d);
    public double BlockSpacing { get => (double)GetValue(BlockSpacingProperty); set => SetValue(BlockSpacingProperty, value); }
    public static readonly BindableProperty LoadImagesProperty = CreateStyleProperty(nameof(LoadImages), true);
    public bool LoadImages { get => (bool)GetValue(LoadImagesProperty); set => SetValue(LoadImagesProperty, value); }

    public event EventHandler<LinkEventArgs>? OnHyperLinkClicked;
    public event EventHandler<Exception>? LinkOpenFailed;
    public event EventHandler<Exception>? RenderFailed;

    public MarkdownView()
    {
        Loaded += (_, _) => { if (Content is null) RequestDocument(); };
        Unloaded += (_, _) =>
        {
            _revision++;
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = null;
            Content = null;
            _document = null;
        };
    }

    private static BindableProperty CreateStyleProperty<T>(string name, T value) => BindableProperty.Create(
        name, typeof(T), typeof(MarkdownView), value, propertyChanged: (view, _, _) => ((MarkdownView)view).Render());
    private static void TextChanged(BindableObject view, object oldValue, object newValue) => ((MarkdownView)view).RequestDocument();

    private void RequestDocument()
    {
        var revision = ++_revision;
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
        var text = MarkdownText ?? "";
        if (MarkdownCache.Shared.TryGet(text, out var cached))
        {
            _document = cached;
            Render();
        }
        else if (text.Length <= 512)
        {
            _document = MarkdownCache.Shared.Get(text);
            Render();
        }
        else
        {
            // Never leave the previous item's content/links visible when a cell is rebound.
            _document = null;
            Content = new Label { Text = text[..Math.Min(text.Length, MarkdownParser.MaxCharacters)], FontSize = FontSize,
                TextColor = TextColor, LineBreakMode = LineBreakMode.WordWrap };
            _pending = new();
            _ = ParseAsync(text, revision, _pending.Token);
        }
    }

    private async Task ParseAsync(string text, int revision, CancellationToken token)
    {
        try
        {
            var document = await Task.Run(async () =>
            {
                await ParseSlots.WaitAsync(token).ConfigureAwait(false);
                try { token.ThrowIfCancellationRequested(); return MarkdownCache.Shared.Get(text); }
                finally { ParseSlots.Release(); }
            }, token);
            if (revision != _revision || token.IsCancellationRequested) return;
            _document = document;
            Render();
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (revision == _revision && !token.IsCancellationRequested)
                RenderFailed?.Invoke(this, error);
        }
    }

    private void Render()
    {
        if (_document is null) return;
        var blocks = _document.Blocks;
        if (blocks.Length == 0) { Content = null; return; }
        if (blocks.Length == 1 && !_document.IsTruncated) { Content = CreateBlock(blocks[0]); return; }
        var stack = new VerticalStackLayout { Spacing = Math.Max(0, BlockSpacing) };
        foreach (var block in blocks) stack.Children.Add(CreateBlock(block));
        if (_document.IsTruncated) stack.Children.Add(new Label { Text = "…", TextColor = TextColor, FontSize = FontSize });
        Content = stack;
    }

    private View CreateBlock(MarkdownBlock block)
    {
        if (block.Kind == BlockKind.Rule) return new BoxView { HeightRequest = 1, Color = TextColor, Opacity = .3 };
        if (block.Kind == BlockKind.Image)
        {
            if (!LoadImages) return new Label { Text = block.Alt, TextColor = TextColor, FontSize = FontSize };
            var image = new Image
            {
                Source = new UriImageSource { Uri = new Uri(block.ImageUrl!), CachingEnabled = true, CacheValidity = TimeSpan.FromDays(7) },
                HeightRequest = Math.Max(1, ImageHeight), Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill
            };
            SemanticProperties.SetDescription(image, block.Alt);
            if (block.Link is not null) AddLink(image.GestureRecognizers, block.Link);
            return image;
        }
        var size = block.Kind == BlockKind.Heading ? FontSize * (block.Level switch
            { 1 => 1.57, 2 => 1.29, 3 => 1.14, _ => 1.0 }) : FontSize;
        var label = new Label
        {
            FontSize = size, TextColor = block.Kind == BlockKind.Heading ? HeadingColor : TextColor,
            LineBreakMode = LineBreakMode.WordWrap,
            Padding = block.Kind is BlockKind.Code or BlockKind.Quote ? new Thickness(8, 4) : new Thickness(0),
            BackgroundColor = block.Kind is BlockKind.Code or BlockKind.Quote ? CodeBackgroundColor : Colors.Transparent
        };
        var headingStyle = block.Kind == BlockKind.Heading ? FontAttributes.Bold : FontAttributes.None;
        if (block.Runs.Length == 1 && block.Runs[0] is { Style: RunStyle.None, Link: null } plain)
        {
            label.Text = plain.Text;
            label.FontAttributes = headingStyle;
            return label;
        }
        var formatted = new FormattedString();
        foreach (var run in block.Runs)
        {
            var span = new Span
            {
                Text = run.Text, FontSize = size,
                TextColor = run.Link is null ? label.TextColor : HyperlinkColor,
                FontAttributes = headingStyle | (run.Style.HasFlag(RunStyle.Bold) ? FontAttributes.Bold : FontAttributes.None)
                    | (run.Style.HasFlag(RunStyle.Italic) ? FontAttributes.Italic : FontAttributes.None),
                TextDecorations = (run.Style.HasFlag(RunStyle.Strike) ? TextDecorations.Strikethrough : TextDecorations.None)
                    | (run.Link is not null ? TextDecorations.Underline : TextDecorations.None),
                BackgroundColor = run.Style.HasFlag(RunStyle.Highlight) ? HighlightColor
                    : run.Style.HasFlag(RunStyle.Code) ? CodeBackgroundColor : Colors.Transparent
            };
            if (run.Style.HasFlag(RunStyle.Code)) span.FontFamily = DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst
                ? "Menlo" : DeviceInfo.Platform == DevicePlatform.WinUI ? "Consolas" : "monospace";
            if (run.Link is not null) AddLink(span.GestureRecognizers, run.Link);
            formatted.Spans.Add(span);
        }
        label.FormattedText = formatted;
        return label;
    }

    private void AddLink(IList<IGestureRecognizer> gestures, string url)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            var args = new LinkEventArgs(url);
            OnHyperLinkClicked?.Invoke(this, args);
            if (args.Handled) return;
            try { await Launcher.Default.OpenAsync(url); }
            catch (Exception error) { LinkOpenFailed?.Invoke(this, error); }
        };
        gestures.Add(tap);
    }
}
