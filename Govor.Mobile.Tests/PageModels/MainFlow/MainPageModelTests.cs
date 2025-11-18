using Moq;
using NUnit.Framework;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.Models;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.PageModels.MainFlow
{
    [TestFixture]
    public class MainPageModelTests
    {
        private Mock<IProfileCacheService> _profileServiceMock;
        private MainPageModel _mainPageModel;

        [SetUp]
        public void SetUp()
        {
            _profileServiceMock = new Mock<IProfileCacheService>();
            _mainPageModel = new MainPageModel(_profileServiceMock.Object);
        }

        [Test]
        public async Task Init_WhenProfileExists_SetsNameFromProfile()
        {
            // Arrange
            var userProfile = new LocalUserProfile { DisplayName = "Test User" };
            _profileServiceMock.Setup(p => p.GetCurrentAsync()).ReturnsAsync(userProfile);

            // Act
            await _mainPageModel.Init();

            // Assert
            Assert.AreEqual("Test User", _mainPageModel.Name);
        }

        [Test]
        public async Task Init_WhenProfileDoesNotExist_NameRemainsGuest()
        {
            // Arrange
            _profileServiceMock.Setup(p => p.GetCurrentAsync()).ReturnsAsync((LocalUserProfile)null);

            // Act
            await _mainPageModel.Init();

            // Assert
            Assert.AreEqual("Гость", _mainPageModel.Name);
        }
    }
}
