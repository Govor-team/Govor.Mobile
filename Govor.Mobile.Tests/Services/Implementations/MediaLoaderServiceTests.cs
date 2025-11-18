using Moq;
using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Govor.Mobile.Models.Common;

namespace Govor.Mobile.Tests.Services.Implementations
{
    [TestFixture]
    public class MediaLoaderServiceTests
    {
        private Mock<IApiClient> _apiClientMock;
        private Mock<ILogger<MediaLoaderService>> _loggerMock;
        private MediaLoaderService _mediaLoaderService;

        [SetUp]
        public void SetUp()
        {
            _apiClientMock = new Mock<IApiClient>();
            _loggerMock = new Mock<ILogger<MediaLoaderService>>();
            _mediaLoaderService = new MediaLoaderService(_apiClientMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task Download_WhenApiCallIsSuccessful_ReturnsSuccessWithStream()
        {
            // Arrange
            var mediaId = Guid.NewGuid();
            var stream = new MemoryStream();
            var fileResult = new FileResult(stream, "test.jpg", "image/jpeg");

            _apiClientMock.Setup(c => c.GetFileStreamAsync(It.IsAny<string>()))
                .ReturnsAsync(Result<FileResult>.Success(fileResult));

            // Act
            var result = await _mediaLoaderService.Download(mediaId);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(stream, result.Value.FileStream);
            Assert.AreEqual("test.jpg", result.Value.FileName);
            Assert.AreEqual("image/jpeg", result.Value.MimeType);
        }

        [Test]
        public async Task Download_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var mediaId = Guid.NewGuid();
            _apiClientMock.Setup(c => c.GetFileStreamAsync(It.IsAny<string>()))
                .ReturnsAsync(Result<FileResult>.Failure("API error"));

            // Act
            var result = await _mediaLoaderService.Download(mediaId);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }

        [Test]
        public async Task Download_WhenApiClientThrowsException_ReturnsFailureAndLogsError()
        {
            // Arrange
            var mediaId = Guid.NewGuid();
            var exception = new Exception("Network error");
            _apiClientMock.Setup(c => c.GetFileStreamAsync(It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _mediaLoaderService.Download(mediaId);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Internal error during download.", result.ErrorMessage);
        }
    }
}
