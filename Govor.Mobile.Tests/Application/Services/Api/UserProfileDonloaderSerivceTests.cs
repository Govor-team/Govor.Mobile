using Moq;
using NUnit.Framework;
using Govor.Mobile.Application.Services.Api;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Models.Results;
using System;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.Application.Services.Api
{
    [TestFixture]
    public class UserProfileDonloaderSerivceTests
    {
        private Mock<IApiClient> _apiClientMock;
        private UserProfileDonloaderSerivce _userProfileDonloaderSerivce;

        [SetUp]
        public void SetUp()
        {
            _apiClientMock = new Mock<IApiClient>();
            _userProfileDonloaderSerivce = new UserProfileDonloaderSerivce(_apiClientMock.Object);
        }

        [Test]
        public async Task GetProfile_WhenApiCallIsSuccessful_ReturnsSuccess()
        {
            // Arrange
            var userProfile = new UserProfile(); // Assuming UserProfile is a class with a parameterless constructor
            _apiClientMock.Setup(c => c.GetAsync<UserProfile>(It.IsAny<string>()))
                .ReturnsAsync(Result<UserProfile>.Success(userProfile));

            // Act
            var result = await _userProfileDonloaderSerivce.GetProfile();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(userProfile, result.Value);
        }

        [Test]
        public async Task GetProfile_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _apiClientMock.Setup(c => c.GetAsync<UserProfile>(It.IsAny<string>()))
                .ReturnsAsync(Result<UserProfile>.Failure("API error"));

            // Act
            var result = await _userProfileDonloaderSerivce.GetProfile();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("API error", result.ErrorMessage);
        }

        [Test]
        public async Task GetProfileByUserId_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile();
            _apiClientMock.Setup(c => c.GetAsync<UserProfile>($"api/profile/dowload/{userId}"))
                .ReturnsAsync(Result<UserProfile>.Success(userProfile));

            // Act
            var result = await _userProfileDonloaderSerivce.GetProfileByUserId(userId);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(userProfile, result.Value);
        }

        [Test]
        public async Task GetProfileByUserId_WithInvalidId_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _apiClientMock.Setup(c => c.GetAsync<UserProfile>($"api/profile/dowload/{userId}"))
                .ReturnsAsync(Result<UserProfile>.Failure("User not found"));

            // Act
            var result = await _userProfileDonloaderSerivce.GetProfileByUserId(userId);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("User not found", result.ErrorMessage);
        }
    }
}
