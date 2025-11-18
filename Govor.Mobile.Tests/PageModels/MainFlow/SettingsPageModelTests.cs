using Moq;
using NUnit.Framework;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.PageModels.MainFlow
{
    [TestFixture]
    public class SettingsPageModelTests
    {
        private Mock<ISessionsService> _sessionsServiceMock;
        private Mock<IDeviceInfoParserService> _infoParserServiceMock;
        private Mock<IUserProfileDonloaderSerivce> _profileDownloaderServiceMock;
        private Mock<IMediaLoaderService> _mediaLoaderServiceMock;
        private SettingsPageModel _settingsPageModel;

        [SetUp]
        public void SetUp()
        {
            _sessionsServiceMock = new Mock<ISessionsService>();
            _infoParserServiceMock = new Mock<IDeviceInfoParserService>();
            _profileDownloaderServiceMock = new Mock<IUserProfileDonloaderSerivce>();
            _mediaLoaderServiceMock = new Mock<IMediaLoaderService>();

            _settingsPageModel = new SettingsPageModel(
                _sessionsServiceMock.Object,
                _profileDownloaderServiceMock.Object,
                _infoParserServiceMock.Object,
                _mediaLoaderServiceMock.Object);
        }

        [Test]
        public async Task Init_WhenSessionsAreLoaded_PopulatesSessionsCollection()
        {
            // Arrange
            var userSessions = new List<UserSession>
            {
                new UserSession { id = Guid.NewGuid(), deviceInfo = "My Phone on Android 11", createdAt = DateTime.Now },
                new UserSession { id = Guid.NewGuid(), deviceInfo = "Desktop on Windows 10", createdAt = DateTime.Now }
            };
            _sessionsServiceMock.Setup(s => s.GetAllSessionsAsync()).ReturnsAsync(Result<List<UserSession>>.Success(userSessions));
            _infoParserServiceMock.Setup(p => p.Parse("My Phone on Android 11")).Returns(new DeviceInfoData { DeviceName = "My Phone", Platform = "Android" });
            _infoParserServiceMock.Setup(p => p.Parse("Desktop on Windows 10")).Returns(new DeviceInfoData { DeviceName = "Desktop", Platform = "Windows" });

            // Act
            await _settingsPageModel.Init();

            // Assert
            Assert.AreEqual(2, _settingsPageModel.Sessions.Count);
            Assert.AreEqual("My Phone", _settingsPageModel.Sessions[0].DeviceName);
            Assert.AreEqual("Desktop", _settingsPageModel.Sessions[1].DeviceName);
        }

        [Test]
        public async Task RemoveSessionAsync_WhenSuccessful_RemovesSessionFromCollection()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var session = new SettingsPageModel.DeviceSession { Id = sessionId, DeviceName = "Test Device" };
            _settingsPageModel.Sessions.Add(session);
            _sessionsServiceMock.Setup(s => s.CloseSessionAsync(sessionId)).ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _settingsPageModel.RemoveSessionCommand.ExecuteAsync(session);

            // Assert
            Assert.IsFalse(_settingsPageModel.Sessions.Contains(session));
        }
    }
}
