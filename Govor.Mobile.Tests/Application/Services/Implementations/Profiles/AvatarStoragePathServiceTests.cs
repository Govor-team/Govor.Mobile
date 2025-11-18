using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations.Profiles;
using Moq;
using Microsoft.Maui.Storage;

namespace Govor.Mobile.Tests.Application.Services.Implementations.Profiles
{
    [TestFixture]
    public class AvatarStoragePathServiceTests
    {
        [Test]
        public void AvatarsFolder_ReturnsCorrectPath()
        {
            // Arrange
            var service = new AvatarStoragePathService();
            var expectedPath = Path.Combine(FileSystem.AppDataDirectory, "avatars");

            // Act
            var actualPath = service.AvatarsFolder;

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }
    }
}
