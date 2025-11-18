using Moq;
using NUnit.Framework;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.Services.Api
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IApiClient> _apiClientMock;
        private Mock<IJwtProviderService> _jwtProviderMock;
        private Mock<IBuilderDeviceInfoString> _deviceInfoStringMock;
        private AuthService _authService;

        [SetUp]
        public void SetUp()
        {
            _apiClientMock = new Mock<IApiClient>();
            _jwtProviderMock = new Mock<IJwtProviderService>();
            _deviceInfoStringMock = new Mock<IBuilderDeviceInfoString>();
            _authService = new AuthService(_apiClientMock.Object, _jwtProviderMock.Object, _deviceInfoStringMock.Object);
        }

        [Test]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessAndSetsAuthenticated()
        {
            // Arrange
            var authResponse = new AuthResponse { accessToken = "access_token", refreshToken = "refresh_token" };
            _apiClientMock.Setup(c => c.PostAsync<AuthResponse>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

            // Act
            var result = await _authService.LoginAsync("testuser", "password");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(_authService.IsAuthenticated);
            _jwtProviderMock.Verify(p => p.SetTokensAsync("access_token", "refresh_token"), Times.Once);
        }

        [Test]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.PostAsync<AuthResponse>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(Result<AuthResponse>.Failure("Invalid credentials"));

            // Act
            var result = await _authService.LoginAsync("testuser", "wrongpassword");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(_authService.IsAuthenticated);
            _jwtProviderMock.Verify(p => p.SetTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessAndSetsAuthenticated()
        {
            // Arrange
            var authResponse = new AuthResponse { accessToken = "access_token", refreshToken = "refresh_token" };
            _apiClientMock.Setup(c => c.PostAsync<AuthResponse>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

            // Act
            var result = await _authService.RegisterAsync("newuser", "password", "invite");

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(_authService.IsAuthenticated);
            _jwtProviderMock.Verify(p => p.SetTokensAsync("access_token", "refresh_token"), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WithInvalidData_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.PostAsync<AuthResponse>(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(Result<AuthResponse>.Failure("Registration failed"));

            // Act
            var result = await _authService.RegisterAsync("newuser", "password", "invalid_invite");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(_authService.IsAuthenticated);
            _jwtProviderMock.Verify(p => p.SetTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LogoutAsync_WhenApiCallIsSuccessful_ClearsJwtAndSetsNotAuthenticated()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(Result.Success());
            await _authService.LoginAsync("testuser", "password"); // Simulate login

            // Act
            await _authService.LogoutAsync();

            // Assert
            _jwtProviderMock.Verify(p => p.ClearAsync(), Times.Once);
            Assert.IsFalse(_authService.IsAuthenticated);
        }

        [Test]
        public void LogoutAsync_WhenApiCallFails_ThrowsLogoutException()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(Result.Failure("API error"));

            // Act & Assert
            Assert.ThrowsAsync<LogoutException>(async () => await _authService.LogoutAsync());
            _jwtProviderMock.Verify(p => p.ClearAsync(), Times.Never);
        }

        [Test]
        public async Task InitializeAsync_WithValidRefreshToken_SetsIsAuthenticatedToTrue()
        {
            // Arrange
            _jwtProviderMock.Setup(p => p.HasValidRefreshToken).Returns(true);

            // Act
            await _authService.InitializeAsync();

            // Assert
            _jwtProviderMock.Verify(p => p.InitializeAsync(), Times.Once);
            Assert.IsTrue(_authService.IsAuthenticated);
        }

        [Test]
        public async Task InitializeAsync_WithInvalidRefreshToken_SetsIsAuthenticatedToFalse()
        {
            // Arrange
            _jwtProviderMock.Setup(p => p.HasValidRefreshToken).Returns(false);

            // Act
            await _authService.InitializeAsync();

            // Assert
            _jwtProviderMock.Verify(p => p.InitializeAsync(), Times.Once);
            Assert.IsFalse(_authService.IsAuthenticated);
        }
    }
}
