namespace Govor.Mobile.Behaviors;

/// <summary>Keeps the whole chat viewport above Android navigation and input windows.</summary>
public sealed class ChatViewportBehavior : Behavior<Grid>
{
#if ANDROID
    private Grid? _layout;
    private Platforms.Android.ChatViewportInsets? _insets;

    protected override void OnAttachedTo(Grid layout)
    {
        base.OnAttachedTo(layout);
        _layout = layout;
        layout.Loaded += Loaded;
        layout.Unloaded += Unloaded;
        layout.HandlerChanging += HandlerChanging;
        layout.HandlerChanged += HandlerChanged;
        Attach();
    }

    private void Loaded(object? sender, EventArgs e)
    {
        // Handler initialization can install MAUI's listener after HandlerChanged.
        // Take ownership only once the native view has finished attaching.
        Detach();
        Attach();
    }
    private void Unloaded(object? sender, EventArgs e) => Detach();
    private void HandlerChanging(object? sender, HandlerChangingEventArgs e) => Detach();
    private void HandlerChanged(object? sender, EventArgs e) => Attach();

    private void Attach()
    {
        if (_insets == null && _layout?.Handler?.PlatformView is global::Android.Views.View view)
            _insets = new Platforms.Android.ChatViewportInsets(_layout, view);
    }

    private void Detach()
    {
        _insets?.Dispose();
        _insets = null;
    }

    protected override void OnDetachingFrom(Grid layout)
    {
        layout.Loaded -= Loaded;
        layout.Unloaded -= Unloaded;
        layout.HandlerChanging -= HandlerChanging;
        layout.HandlerChanged -= HandlerChanged;
        Detach();
        _layout = null;
        base.OnDetachingFrom(layout);
    }
#endif
}
