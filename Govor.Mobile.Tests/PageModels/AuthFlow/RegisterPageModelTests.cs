using Moq;
using NUnit.Framework;
using Govor.Mobile.PageModels.AuthFlow;
using System;
using System.Threading.Tasks;

namespace Govor.Mobile.Tests.PageModels.AuthFlow
{
    [TestFixture]
    public class RegisterPageModelTests
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private RegisterPageModel _registerPageModel;

        [SetUp]
        public void SetUp()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _registerPageModel = new RegisterPageModel(_serviceProviderMock.Object);
        }

        [Test]
        public void CanNext_WithValidInput_ReturnsTrue()
        {
            // Arrange
            _registerPageModel.Name = "newuser";
            _registerPageModel.Password = "password";
            _registerPageModel.IsBusy = false;

            // Assert
            Assert.IsTrue(_registerPageModel.NextCommand.CanExecute(null));
        }

        [TestCase("", "password", false)]
        [TestCase("user", "", false)]
        [TestCase("user", "password", true)]
        public void CanNext_WithInvalidInput_ReturnsFalse(string name, string password, bool isBusy)
        {
            // Arrange
            _registerPageModel.Name = name;
            _registerPageModel.Password = password;
            _registerPageModel.IsBusy = isBusy;

            // Assert
            Assert.IsFalse(_registerPageModel.NextCommand.CanExecute(null));
        }

        [Test]
        public void TogglePasswordVisibility_ChangesIsPasswordHiddenAndEyeIcon()
        {
            // Arrange
            var initialVisibility = _registerPageModel.IsPasswordHidden;
            var initialIcon = _registerPageModel.EyeIcon;

            // Act
            _registerPageModel.TogglePasswordVisibilityCommand.Execute(null);

            // Assert
            Assert.AreNotEqual(initialVisibility, _registerPageModel.IsPasswordHidden);
            Assert.AreNotEqual(initialIcon, _registerPageModel.EyeIcon);
        }
    }
}
