using Govor.Mobile.Utilities;

namespace Govor.Mobile.Tests;

public class ChatViewportTests
{
    [Test]
    public void KeyboardCycleRestoresNavigationSpace()
    {
        Assert.That(ChatViewportGeometry.BottomOverlap(1000, 1000, 48, 0), Is.EqualTo(48));
        Assert.That(ChatViewportGeometry.BottomOverlap(1000, 1000, 48, 350), Is.EqualTo(350));
        Assert.That(ChatViewportGeometry.BottomOverlap(1000, 1000, 48, 0), Is.EqualTo(48));
    }

    [TestCase(650, 350, 0)] // AdjustResize has already excluded the keyboard.
    [TestCase(700, 350, 50)] // Only the remaining overlap needs padding.
    [TestCase(952, 0, 0)]   // Parent already excludes the navigation bar.
    [TestCase(1000, 48, 48)] // Collapsed keyboard toolbar shares the navigation region.
    public void AlreadyExcludedSpaceIsNotCountedTwice(int viewportBottom, int keyboard, int expected)
        => Assert.That(ChatViewportGeometry.BottomOverlap(1000, viewportBottom, 48, keyboard), Is.EqualTo(expected));
}
