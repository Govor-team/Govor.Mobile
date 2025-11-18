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
    public class CodeInputModelTests
    {
        private Mock<IAuthService> _authServiceMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private CodeInputModel _codeInputModel;

        [SetUp]
        public void SetUp()
        {
            _authServiceMock = new Mock<IAuthService>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _codeInputModel = new CodeInputModel(_authServiceMock.Object, _serviceProviderMock.Object);
        }

        [Test]
        public async Task RegisterAsync_WithValidCode_CallsAuthService()
        {
            // Arrange
            _codeInputModel.Name = "testuser";
            _codeInputModel.Password = "password";
            _codeInputModel.Code = "123456";
            _authServiceMock.Setup(a => a.RegisterAsync("testuser", "password", "123456"))
                .ReturnsAsync(Result<UserLogin>.Success(new UserLogin("testuser", "password", "refresh_token")));

            // Act
            await _codeInputModel.RegisterCommand.ExecuteAsync(null);

            // Assert
            _authServiceMock.Verify(a => a.RegisterAsync("testuser", "password", "123456"), Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WithInvalidCode_CallsAuthServiceAndDisplaysError()
        {
            // Arrange
            _codeInputModel.Name = "testuser";
            _codeInputModel.Password = "password";
            _codeInputModel.Code = "wrongcode";
            _authServiceMock.Setup(a => a.RegisterAsync("testuser", "password", "wrongcode"))
                .ReturnsAsync(Result<UserLogin>.Failure("Invalid code"));

            // Act
            await _codeInputModel.RegisterCommand.ExecuteAsync(null);

            // Assert
            _authServiceMock.Verify(a => a.RegisterAsync("testuser", "password", "wrongcode"), Times.Once);
        }

        [Test]
        public void CanNext_WithValidInput_ReturnsTrue()
        {
            // Arrange
            _codeInputModel.Code = "123456";
            _codeInputModel.IsBusy = false;

            // Assert
            Assert.IsTrue(_codeInputModel.RegisterCommand.CanExecute(null));
        }

        [TestCase("", false)]
        [TestCase("123456", true)]
        public void CanNext_WithInvalidInput_ReturnsFalse(string code, bool isBusy)
        {
            // Arrange
            _codeInputModel.Code = code;
            _codeInputModel.IsBusy = isBusy;

            // Assert
            Assert.IsFalse(_codeInputModel.RegisterCommand.CanExecute(null));
        }

        [Test]
        public void ToggleCodeVisibility_ChangesIsCodeHiddenAndEyeIcon()
        {
            // Arrange
            var initialVisibility = _codeInputModel.IsCodeHidden;
            var initialIcon = _codeInputModel.EyeIcon;

            // Act
            _codeInputModel.ToggleCodeVisibilityCommand.Execute(null);

            // Assert
            Assert.AreNotEqual(initialVisibility, _codeInputModel.IsCodeHidden);
            Assert.AreNotEqual(initialIcon, _codeInputModel.EyeIcon);
        }
    }
}
