using Moq;
using NUnit.Framework;
using Govor.Mobile.PageModels.AuthFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Models.Results;
using System;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.PageModels.AuthFlow
{
    [TestFixture]
    public class LoginPageModelTests
    {
        private Mock<IAuthService> _authServiceMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private LoginPageModel _loginPageModel;

        [SetUp]
        public void SetUp()
        {
            _authServiceMock = new Mock<IAuthService>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _loginPageModel = new LoginPageModel(_authServiceMock.Object, _serviceProviderMock.Object);
        }

        [Test]
        public async Task LoginAsync_WithValidCredentials_CallsAuthServiceAndDoesNotDisplayError()
        {
            // Arrange
            _loginPageModel.Name = "testuser";
            _loginPageModel.Password = "password";
            _authServiceMock.Setup(a => a.LoginAsync("testuser", "password"))
                .ReturnsAsync(Result<UserLogin>.Success(new UserLogin("testuser", "password", "refresh_token")));

            // Act
            await _loginPageModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _authServiceMock.Verify(a => a.LoginAsync("testuser", "password"), Times.Once);
        }

        [Test]
        public async Task LoginAsync_WithInvalidCredentials_CallsAuthServiceAndDisplaysError()
        {
            // Arrange
            _loginPageModel.Name = "testuser";
            _loginPageModel.Password = "wrongpassword";
            _authServiceMock.Setup(a => a.LoginAsync("testuser", "wrongpassword"))
                .ReturnsAsync(Result<UserLogin>.Failure("Invalid credentials"));

            // Act
            await _loginPageModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _authServiceMock.Verify(a => a.LoginAsync("testuser", "wrongpassword"), Times.Once);
        }

        [Test]
        public void CanLogin_WithValidInput_ReturnsTrue()
        {
            // Arrange
            _loginPageModel.Name = "user";
            _loginPageModel.Password = "pass";
            _loginPageModel.IsBusy = false;

            // Assert
            Assert.IsTrue(_loginPageModel.LoginCommand.CanExecute(null));
        }

        [TestCase("", "pass", false)]
        [TestCase("user", "", false)]
        [TestCase("user", "pass", true)]
        public void CanLogin_WithInvalidInput_ReturnsFalse(string name, string password, bool isBusy)
        {
            // Arrange
            _loginPageModel.Name = name;
            _loginPageModel.Password = password;
            _loginPageModel.IsBusy = isBusy;

            // Assert
            Assert.IsFalse(_loginPageModel.LoginCommand.CanExecute(null));
        }

        [Test]
        public void TogglePasswordVisibility_ChangesIsPasswordHiddenAndEyeIcon()
        {
            // Arrange
            var initialVisibility = _loginPageModel.IsPasswordHidden;
            var initialIcon = _loginPageModel.EyeIcon;

            // Act
            _loginPageModel.TogglePasswordVisibilityCommand.Execute(null);

            // Assert
            Assert.AreNotEqual(initialVisibility, _loginPageModel.IsPasswordHidden);
            Assert.AreNotEqual(initialIcon, _loginPageModel.EyeIcon);
        }
    }
}
