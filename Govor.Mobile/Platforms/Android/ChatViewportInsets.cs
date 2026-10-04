using Android.Views;
using AndroidX.Core.View;
using Govor.Mobile.Utilities;
using AView = Android.Views.View;

namespace Govor.Mobile.Platforms.Android;

/// <summary>Uses unconsumed root insets, including the navigation bar hidden by the IME.</summary>
internal sealed class ChatViewportInsets : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    private readonly Grid _layout;
    private readonly AView _view;
    private readonly Thickness _originalPadding;
    private readonly int[] _viewLocation = new int[2];
    private readonly int[] _rootLocation = new int[2];
    private bool _disposed;

    public ChatViewportInsets(Grid layout, AView view)
    {
        _layout = layout;
        _view = view;
        _originalPadding = layout.Padding;
        view.ViewTreeObserver!.GlobalLayout += OnGlobalLayout;
        ViewCompat.SetOnApplyWindowInsetsListener(view, this);
        ViewCompat.RequestApplyInsets(view);
    }

    public WindowInsetsCompat? OnApplyWindowInsets(AView? view, WindowInsetsCompat? insets)
    {
        Update();
        // This viewport has already applied all four edges. Passing them to children
        // makes their first measure reserve the same status/navigation space again.
        return WindowInsetsCompat.Consumed;
    }

    private void OnGlobalLayout(object? sender, EventArgs e) => Update();

    private void Update()
    {
        if (_disposed || !_view.IsAttachedToWindow || _view.Height == 0) return;
        var root = _view.RootView;
        var insets = ViewCompat.GetRootWindowInsets(_view);
        if (root == null || insets == null) return;

        // Visibility-based navigation insets can become zero while switching keyboards.
        // The reserved navigation region is still needed when the keyboard is dismissed.
        var bars = insets.GetInsetsIgnoringVisibility(WindowInsetsCompat.Type.SystemBars()
            | WindowInsetsCompat.Type.DisplayCutout());
        var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
        if (bars == null || ime == null) return;
        root.GetLocationOnScreen(_rootLocation);
        _view.GetLocationOnScreen(_viewLocation);
        var windowLeft = _rootLocation[0];
        var windowTop = _rootLocation[1];
        var windowRight = windowLeft + root.Width;
        var windowBottom = windowTop + root.Height;
        if (OperatingSystem.IsAndroidVersionAtLeast(30)
            && Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.WindowManager?.CurrentWindowMetrics?.Bounds is { } bounds)
        {
            // DecorView itself may resize for the IME. Window metrics retain the full
            // activity bounds, including in split-screen and after device rotation.
            windowLeft = bounds.Left; windowTop = bounds.Top;
            windowRight = bounds.Right; windowBottom = bounds.Bottom;
        }
        var density = _view.Resources?.DisplayMetrics?.Density ?? 1;
        var bottom = ChatViewportGeometry.BottomOverlap(windowBottom,
            _viewLocation[1] + _view.Height, bars.Bottom, ime.Bottom);

        // Subtract space already excluded by a resized parent, rather than adding IME
        // height blindly (which doubles the keyboard gap under AdjustResize).
        var padding = new Thickness(
            _originalPadding.Left + Math.Max(0, windowLeft + bars.Left - _viewLocation[0]) / density,
            _originalPadding.Top + Math.Max(0, windowTop + bars.Top - _viewLocation[1]) / density,
            _originalPadding.Right + Math.Max(0, _viewLocation[0] + _view.Width
                - (windowRight - bars.Right)) / density,
            _originalPadding.Bottom + bottom / density);
        if (_layout.Padding != padding) _layout.Padding = padding;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            if (_view.ViewTreeObserver is { IsAlive: true } observer)
                observer.GlobalLayout -= OnGlobalLayout;
            ViewCompat.SetOnApplyWindowInsetsListener(_view, null);
            _layout.Padding = _originalPadding;
        }
        base.Dispose(disposing);
    }
}
