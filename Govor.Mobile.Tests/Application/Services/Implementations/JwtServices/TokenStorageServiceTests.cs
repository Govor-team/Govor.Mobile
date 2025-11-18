using Moq;
using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations.JwtServices;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Govor.Mobile.Application.Services.Interfaces.Wrappers;

namespace Govor.Mobile.Tests.Application.Services.Implementations.JwtServices
{
    [TestFixture]
    public class TokenStorageServiceTests
    {
        private Mock<ILogger<TokenStorageService>> _loggerMock;
        private Mock<ISecureStorage> _secureStorageMock;
        private TokenStorageService _tokenStorageService;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<TokenStorageService>>();
            _secureStorageMock = new Mock<ISecureStorage>();
            _tokenStorageService = new TokenStorageService(_loggerMock.Object, _secureStorageMock.Object);
        }

        [Test]
        public async Task SaveRefreshTokenAsync_WithValidToken_SavesToken()
        {
            // Arrange
            var token = "refresh_token";

            // Act
            await _tokenStorageService.SaveRefreshTokenAsync(token);

            // Assert
            _secureStorageMock.Verify(s => s.SetAsync("RefreshToken", token), Times.Once);
        }

        [Test]
        public async Task GetRefreshTokenAsync_WhenTokenExists_ReturnsToken()
        {
            // Arrange
            var token = "refresh_token";
            _secureStorageMock.Setup(s => s.GetAsync("RefreshToken")).ReturnsAsync(token);

            // Act
            var retrievedToken = await _tokenStorageService.GetRefreshTokenAsync();

            // Assert
            Assert.AreEqual(token, retrievedToken);
        }

        [Test]
        public void DeleteRefreshToken_WhenTokenExists_DeletesToken()
        {
            // Act
            _tokenStorageService.DeleteRefreshToken();

            // Assert
            _secureStorageMock.Verify(s => s.Remove("RefreshToken"), Times.Once);
        }
    }
}
