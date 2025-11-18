using Moq;
using NUnit.Framework;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Services.Interfaces;
using Govor.Mobile.Application.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Moq.Protected;
using System.Threading;

namespace Govor.Mobile.Tests.Application.Services.Api.Base
{
    [TestFixture]
    public class ApiClientTests
    {
        private Mock<IServerIpProvader> _ipProvaderMock;
        private Mock<IJwtProviderService> _jwtProviderMock;
        private Mock<ILogger<ApiClient>> _loggerMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private ApiClient _apiClient;

        [SetUp]
        public void SetUp()
        {
            _ipProvaderMock = new Mock<IServerIpProvader>();
            _jwtProviderMock = new Mock<IJwtProviderService>();
            _loggerMock = new Mock<ILogger<ApiClient>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            _ipProvaderMock.Setup(p => p.IP).Returns("http://localhost");

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _apiClient = new ApiClient(_ipProvaderMock.Object, _jwtProviderMock.Object, _loggerMock.Object, _httpClient);
        }

        [Test]
        public async Task GetAsync_WhenSuccessful_ReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new { id = 1 }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _apiClient.GetAsync<object>("/test");

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task GetAsync_WhenUnauthorizedAndRefreshFails_ReturnsUnauthorized()
        {
            // Arrange
            var unauthorizedResponse = new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };
            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse);
            _jwtProviderMock.Setup(p => p.RefreshAsync()).ReturnsAsync(Govor.Mobile.Application.Utilities.Result<bool>.Failure("Refresh failed"));

            // Act
            var result = await _apiClient.GetAsync<object>("/test");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Test]
        public async Task GetAsync_WhenUnauthorizedAndRefreshSucceeds_ReturnsSuccess()
        {
            // Arrange
            var unauthorizedResponse = new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };
            var successResponse = new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonSerializer.Serialize(new { id = 1 })) };
            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse)
                .ReturnsAsync(successResponse);
            _jwtProviderMock.Setup(p => p.RefreshAsync()).ReturnsAsync(Govor.Mobile.Application.Utilities.Result<bool>.Success(true));

            // Act
            var result = await _apiClient.GetAsync<object>("/test");

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task PostAsync_WhenSuccessful_ReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new { id = 1 }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _apiClient.PostAsync<object>("/test", new { });

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task PutAsync_WhenSuccessful_ReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new { id = 1 }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _apiClient.PutAsync<object>("/test", new { });

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task DeleteAsync_WhenSuccessful_ReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _apiClient.DeleteAsync("/test");

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [Test]
        public async Task GetFileStreamAsync_WhenSuccessful_ReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(new MemoryStream())
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _apiClient.GetFileStreamAsync("/test");

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }
    }
}
