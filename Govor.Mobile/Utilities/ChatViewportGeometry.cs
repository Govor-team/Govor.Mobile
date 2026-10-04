namespace Govor.Mobile.Utilities;

public static class ChatViewportGeometry
{
    public static int BottomOverlap(int windowBottom, int viewportBottom, int navigationHeight, int keyboardHeight)
        => Math.Max(0, viewportBottom - (windowBottom - Math.Max(navigationHeight, keyboardHeight)));
}
