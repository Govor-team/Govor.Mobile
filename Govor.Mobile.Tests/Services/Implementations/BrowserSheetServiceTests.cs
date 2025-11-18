using Moq;
using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
// Assuming BrowserBottomSheet is a class in this namespace
// using Govor.Mobile.Controls;

namespace Govor.Mobile.Tests.Services.Implementations
{
    // We need a mockable version of BrowserBottomSheet for testing.
    // Moq can only mock interfaces or virtual/abstract methods.
    // If BrowserBottomSheet is a sealed class or its methods are not virtual,
    // we would need to wrap it or use a different approach.
    // For this test, we'll create a simple fake class.
    public class FakeBrowserBottomSheet
    {
        public string LastOpenedUrl { get; private set; }
        public bool IsOpenCalled { get; private set; }

        public virtual void Open(string url)
        {
            LastOpenedUrl = url;
            IsOpenCalled = true;
        }
    }

    [TestFixture]
    public class BrowserSheetServiceTests
    {
        private BrowserSheetService _browserSheetService;
        private Mock<FakeBrowserBottomSheet> _sheetMock;

        [SetUp]
        public void SetUp()
        {
            _browserSheetService = new BrowserSheetService();
            _sheetMock = new Mock<FakeBrowserBottomSheet>();
        }

        [Test]
        public void Open_WhenSheetIsInitialized_CallsSheetOpen()
        {
            // Arrange
            var url = "https://example.com";
            _browserSheetService.Initialize(_sheetMock.Object);

            // Act
            _browserSheetService.Open(url);

            // Assert
            _sheetMock.Verify(s => s.Open(url), Times.Once);
        }

        [Test]
        public void Open_WhenSheetIsNotInitialized_DoesNotThrow()
        {
            // Arrange
            var url = "https://example.com";

            // Act & Assert
            Assert.DoesNotThrow(() => _browserSheetService.Open(url));
            _sheetMock.Verify(s => s.Open(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Close_DoesNotThrow()
        {
            // The method is commented out, so this test just ensures it doesn't crash.
            // Arrange
            _browserSheetService.Initialize(_sheetMock.Object);

            // Act & Assert
            Assert.DoesNotThrow(() => _browserSheetService.Close());
        }
    }
}
