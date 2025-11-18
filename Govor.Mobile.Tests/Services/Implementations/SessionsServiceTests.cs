using Moq;
using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.Services.Implementations
{
    [TestFixture]
    public class SessionsServiceTests
    {
        private Mock<IApiClient> _apiClientMock;
        private SessionsService _sessionsService;

        [SetUp]
        public void SetUp()
        {
            _apiClientMock = new Mock<IApiClient>();
            _sessionsService = new SessionsService(_apiClientMock.Object);
        }

        [Test]
        public async Task CloseAllSessionsAsync_WhenApiCallIsSuccessful_ReturnsSuccess()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync("api/session/close/all")).ReturnsAsync(Result.Success());

            // Act
            var result = await _sessionsService.CloseAllSessionsAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public async Task CloseAllSessionsAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync("api/session/close/all")).ReturnsAsync(Result.Failure("API error"));

            // Act
            var result = await _sessionsService.CloseAllSessionsAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }

        [Test]
        public async Task CloseCurrentSessionAsync_WhenApiCallIsSuccessful_ReturnsSuccess()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync("api/session/close/")).ReturnsAsync(Result.Success());

            // Act
            var result = await _sessionsService.CloseCurrentSessionAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public async Task CloseCurrentSessionAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.DeleteAsync("api/session/close/")).ReturnsAsync(Result.Failure("API error"));

            // Act
            var result = await _sessionsService.CloseCurrentSessionAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }

        [Test]
        public async Task CloseSessionAsync_WhenApiCallIsSuccessful_ReturnsSuccess()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _apiClientMock.Setup(c => c.DeleteAsync($"api/session/close/{sessionId}")).ReturnsAsync(Result.Success());

            // Act
            var result = await _sessionsService.CloseSessionAsync(sessionId);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public async Task CloseSessionAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _apiClientMock.Setup(c => c.DeleteAsync($"api/session/close/{sessionId}")).ReturnsAsync(Result.Failure("API error"));

            // Act
            var result = await _sessionsService.CloseSessionAsync(sessionId);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }

        [Test]
        public async Task GetAllSessionsAsync_WhenApiCallIsSuccessful_ReturnsSuccessWithSessions()
        {
            // Arrange
            var sessions = new List<UserSession> { new UserSession(), new UserSession() };
            _apiClientMock.Setup(c => c.GetAsync<List<UserSession>>("api/session/all")).ReturnsAsync(Result<List<UserSession>>.Success(sessions));

            // Act
            var result = await _sessionsService.GetAllSessionsAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(sessions, result.Value);
        }

        [Test]
        public async Task GetAllSessionsAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.GetAsync<List<UserSession>>("api/session/all")).ReturnsAsync(Result<List<UserSession>>.Failure("API error"));

            // Act
            var result = await _sessionsService.GetAllSessionsAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }
    }
}
