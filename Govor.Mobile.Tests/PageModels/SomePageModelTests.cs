using Moq;
using NUnit.Framework;
using Govor.Mobile.PageModels;
using Govor.Mobile.Services.Api;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.PageModels
{
    [TestFixture]
    public class SomePageModelTests
    {
        private Mock<IAuthService> _authServiceMock;
        private SomePageModel _somePageModel;

        [SetUp]
        public void SetUp()
        {
            _authServiceMock = new Mock<IAuthService>();
            _somePageModel = new SomePageModel(_authServiceMock.Object);
        }

        [Test]
        public async Task LogoutAsync_CallsAuthServiceLogout()
        {
            // Act
            await _somePageModel.LogoutCommand.ExecuteAsync(null);

            // Assert
            _authServiceMock.Verify(a => a.LogoutAsync(), Times.Once);
        }
    }
}
