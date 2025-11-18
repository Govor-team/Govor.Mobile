using Moq;
using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations.Profiles;
using Govor.Mobile.Application.Services.Interfaces.Profiles;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.Application.Services.Implementations.Profiles
{
    [TestFixture]
    public class AvatarSaverServiceTests
    {
        private Mock<IAvatarStoragePath> _storagePathMock;
        private Mock<ILogger<AvatarSaverService>> _loggerMock;
        private AvatarSaverService _avatarSaverService;
        private string _testFolder;

        [SetUp]
        public void SetUp()
        {
            _storagePathMock = new Mock<IAvatarStoragePath>();
            _loggerMock = new Mock<ILogger<AvatarSaverService>>();
            _testFolder = Path.Combine(Path.GetTempPath(), "AvatarTests");
            Directory.CreateDirectory(_testFolder);
            _storagePathMock.Setup(s => s.AvatarsFolder).Returns(_testFolder);
            _avatarSaverService = new AvatarSaverService(_storagePathMock.Object, _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_testFolder, true);
        }

        [Test]
        public async Task SaveAvatarAsync_WithValidStream_SavesAvatar()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileName = "test.jpg";
            var stream = new MemoryStream();
            var expectedPath = Path.Combine(_testFolder, $"{userId}.jpg");

            // Act
            var savedPath = await _avatarSaverService.SaveAvatarAsync(userId, fileName, stream);

            // Assert
            Assert.AreEqual(expectedPath, savedPath);
            Assert.IsTrue(File.Exists(savedPath));
        }

        [Test]
        public void DeleteAvatarAsync_WhenFileExists_DeletesAvatar()
        {
            // Arrange
            var fileName = "test.jpg";
            var filePath = Path.Combine(_testFolder, fileName);
            File.Create(filePath).Close();

            // Act
            _avatarSaverService.DeleteAvatarAsync(fileName);

            // Assert
            Assert.IsFalse(File.Exists(filePath));
        }
    }
}
