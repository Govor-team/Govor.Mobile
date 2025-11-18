using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations;
using Moq;
using Govor.Mobile.Application.Services.Api;
using Govor.Mobile.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Govor.Mobile.Application.Data;
using System;
using System.Threading.Tasks;
using Govor.Mobile.Application.Services.Interfaces.Profiles;
using Microsoft.EntityFrameworkCore;
using Govor.Mobile.Application.Models;
using System.Collections.Generic;
using System.Linq;
using Govor.Mobile.Application.Models.Results;

namespace Govor.Mobile.Tests.Application.Services.Implementations
{
    [TestFixture]
    public class ProfileCacheServiceTests
    {
        private Mock<IUserProfileDonloaderSerivce> _downloaderMock;
        private Mock<IMediaLoaderService> _mediaLoaderMock;
        private Mock<ILogger<ProfileCacheService>> _loggerMock;
        private DbContextOptions<GovorDbContext> _dbContextOptions;
        private GovorDbContext _context;
        private ProfileCacheService _profileCacheService;

        [SetUp]
        public void SetUp()
        {
            _downloaderMock = new Mock<IUserProfileDonloaderSerivce>();
            _mediaLoaderMock = new Mock<IMediaLoaderService>();
            _loggerMock = new Mock<ILogger<ProfileCacheService>>();
            _dbContextOptions = new DbContextOptionsBuilder<GovorDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _context = new GovorDbContext(_dbContextOptions);
            _profileCacheService = new ProfileCacheService(
                _context,
                _downloaderMock.Object,
                _mediaLoaderMock.Object,
                _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetCurrentAsync_WhenCacheIsNotEmpty_ReturnsCachedProfile()
        {
            // Arrange
            var profile = new LocalUserProfile { UserId = Guid.NewGuid(), DisplayName = "Test User" };
            await _context.UserProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _profileCacheService.GetCurrentAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(profile.UserId, result.UserId);
        }

        [Test]
        public async Task GetProfileByUserId_WhenProfileIsInDb_ReturnsProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var profile = new LocalUserProfile { UserId = userId, DisplayName = "Test User" };
            await _context.UserProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _profileCacheService.GetProfileByUserId(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.UserId);
        }

        [Test]
        public async Task GetProfileByUserId_WhenProfileIsNotInDb_FetchesAndSavesProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile { UserId = userId, DisplayName = "Test User" };
            _downloaderMock.Setup(d => d.GetProfileByUserId(userId)).ReturnsAsync(Result<UserProfile>.Success(userProfile));

            // Act
            var result = await _profileCacheService.GetProfileByUserId(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.UserId);
            var dbProfile = await _context.UserProfiles.FindAsync(userId);
            Assert.IsNotNull(dbProfile);
        }

        [Test]
        public async Task RefreshAsync_WhenSuccessful_UpdatesProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userProfile = new UserProfile { UserId = userId, DisplayName = "Updated User" };
            _downloaderMock.Setup(d => d.GetProfile()).ReturnsAsync(Result<UserProfile>.Success(userProfile));
            var existingProfile = new LocalUserProfile { UserId = userId, DisplayName = "Test User" };
            await _context.UserProfiles.AddAsync(existingProfile);
            await _context.SaveChangesAsync();

            // Act
            var result = await _profileCacheService.RefreshAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated User", result.DisplayName);
            var dbProfile = await _context.UserProfiles.FindAsync(userId);
            Assert.AreEqual("Updated User", dbProfile.DisplayName);
        }
    }
}
