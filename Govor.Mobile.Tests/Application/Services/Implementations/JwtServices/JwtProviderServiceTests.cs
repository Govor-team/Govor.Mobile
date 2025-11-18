using Moq;
using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations.JwtServices;
using Govor.Mobile.Application.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Govor.Mobile.Application.Models.Responses;
using Moq.Protected;
using System.Threading;

namespace Govor.Mobile.Tests.Application.Services.Implementations.JwtServices
{
    [TestFixture]
    public class JwtProviderServiceTests
    {
        private Mock<ITokenStorageService> _tokenStorageMock;
        private Mock<ILogger<JwtProviderService>> _loggerMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private JwtProviderService _jwtProviderService;

        [SetUp]
        public void SetUp()
        {
            _tokenStorageMock = new Mock<ITokenStorageService>();
            _loggerMock = new Mock<ILogger<JwtProviderService>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _jwtProviderService = new JwtProviderService(_loggerMock.Object, _tokenStorageMock.Object, _httpClient);
        }

        [Test]
        public async Task RefreshAsync_WhenSuccessful_ReturnsSuccessAndSetsTokens()
        {
            // Arrange
            var refreshResponse = new RefreshResponse { accessToken = "new_access_token", refreshToken = "new_refresh_token" };
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(refreshResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _jwtProviderService.RefreshAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("new_access_token", _jwtProviderService.AccessToken);
            Assert.AreEqual("new_refresh_token", _jwtProviderService.RefreshToken);
            _tokenStorageMock.Verify(s => s.SaveRefreshTokenAsync("new_refresh_token"), Times.Once);
        }

        [Test]
        public async Task SetTokensAsync_WithValidTokens_SavesRefreshToken()
        {
            // Act
            await _jwtProviderService.SetTokensAsync("access_token", "refresh_token");

            // Assert
            _tokenStorageMock.Verify(s => s.SaveRefreshTokenAsync("refresh_token"), Times.Once);
        }

        [Test]
        public async Task ClearAsync_DeletesRefreshToken()
        {
            // Act
            await _jwtProviderService.ClearAsync();

            // Assert
            _tokenStorageMock.Verify(s => s.DeleteRefreshToken(), Times.Once);
        }
    }
}
