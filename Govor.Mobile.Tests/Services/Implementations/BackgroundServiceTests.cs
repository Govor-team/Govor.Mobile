using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
using Moq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;

namespace Govor.Mobile.Tests.Services.Implementations
{
    public interface IApplication
    {
        AppTheme RequestedTheme { get; }
    }

    [TestFixture]
    public class BackgroundServiceTests
    {
        private BackgroundService _backgroundService;
        private Mock<IApplication> _applicationMock;

        [SetUp]
        public void SetUp()
        {
            _applicationMock = new Mock<IApplication>();
            _backgroundService = new BackgroundService();
            Preferences.Clear();
        }

        [Test]
        public void GetBackgroundImage_WhenUserPreferenceIsSet_ReturnsPreference()
        {
            // Arrange
            var expectedPath = "user_selected_background.png";
            Preferences.Set("UserBackgroundImage", expectedPath);

            // Act
            var actualPath = _backgroundService.GetBackgroundImage();

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void GetBackgroundImage_WhenNoPreferenceAndAppIsDarkTheme_ReturnsDarkThemeDefault()
        {
            // Arrange
            _applicationMock.Setup(a => a.RequestedTheme).Returns(AppTheme.Dark);

            // Act
            var actualPath = _backgroundService.GetBackgroundImage(_applicationMock.Object);

            // Assert
            Assert.AreEqual("background_girls.png", actualPath);
        }

        [Test]
        public void GetBackgroundImage_WhenNoPreferenceAndAppIsLightTheme_ReturnsLightThemeDefault()
        {
            // Arrange
            _applicationMock.Setup(a => a.RequestedTheme).Returns(AppTheme.Light);

            // Act
            var actualPath = _backgroundService.GetBackgroundImage(_applicationMock.Object);

            // Assert
            Assert.AreEqual("background_flag.png", actualPath);
        }

        [Test]
        public void SetUserBackground_SetsPreference()
        {
            // Arrange
            var imagePath = "new_background.png";

            // Act
            _backgroundService.SetUserBackground(imagePath);

            // Assert
            Assert.AreEqual(imagePath, Preferences.Get("UserBackgroundImage", null));
        }



        [Test]
        public void ClearUserBackground_RemovesPreference()
        {
            // Arrange
            Preferences.Set("UserBackgroundImage", "some_path.png");

            // Act
            _backgroundService.ClearUserBackground();

            // Assert
            Assert.IsFalse(Preferences.ContainsKey("UserBackgroundImage"));
        }
    }
}
