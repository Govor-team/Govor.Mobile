using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations;
using Moq;
using Microsoft.Maui.ApplicationModel;
using Govor.Mobile.Application.Services.Interfaces.Wrappers;

namespace Govor.Mobile.Tests.Application.Services.Implementations
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
        private Mock<IPreferences> _preferencesMock;

        [SetUp]
        public void SetUp()
        {
            _applicationMock = new Mock<IApplication>();
            _preferencesMock = new Mock<IPreferences>();
            _backgroundService = new BackgroundService(_preferencesMock.Object);
        }

        [Test]
        public void GetBackgroundImage_WhenUserPreferenceIsSet_ReturnsPreference()
        {
            // Arrange
            var expectedPath = "user_selected_background.png";
            _preferencesMock.Setup(p => p.ContainsKey("UserBackgroundImage")).Returns(true);
            _preferencesMock.Setup(p => p.Get("UserBackgroundImage", null)).Returns(expectedPath);

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
            _preferencesMock.Verify(p => p.Set("UserBackgroundImage", imagePath), Times.Once);
        }



        [Test]
        public void ClearUserBackground_RemovesPreference()
        {
            // Act
            _backgroundService.ClearUserBackground();

            // Assert
            _preferencesMock.Verify(p => p.Remove("UserBackgroundImage"), Times.Once);
        }
    }
}
