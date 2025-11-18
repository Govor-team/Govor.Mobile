using NUnit.Framework;
using Govor.Mobile.PageModels;
using Microsoft.Maui.Controls;

namespace Govor.Mobile.Tests.PageModels
{
    [TestFixture]
    public class WebBrowserPageModelTests
    {
        private WebBrowserPageModel _webBrowserPageModel;

        [SetUp]
        public void SetUp()
        {
            _webBrowserPageModel = new WebBrowserPageModel();
        }

        [TestCase("google.com", "https://google.com")]
        [TestCase("https://example.com", "https://example.com")]
        [TestCase("search query", "https://www.google.com/search?q=search%20query")]
        public void OnUrlEntered_FormatsUrlCorrectly(string input, string expected)
        {
            // Arrange
            _webBrowserPageModel.Url = input;

            // Act
            _webBrowserPageModel.OnUrlEntered();

            // Assert
            Assert.AreEqual(expected, _webBrowserPageModel.CurrentUrl);
        }

        [Test]
        public void OnNavigated_UpdatesShortAddressAndTitle()
        {
            // Arrange
            var eventArgs = new WebNavigatedEventArgs(WebNavigationEvent.NewPage, "https://govor.gitbook.io/wiki/govor-api/", "https://govor.gitbook.io/wiki/govor-api/", WebNavigationResult.Success);

            // Act
            _webBrowserPageModel.OnNavigated(null, eventArgs);

            // Assert
            Assert.AreEqual("govor.gitbook.io", _webBrowserPageModel.ShortAddress);
            Assert.AreEqual("Документация Govor", _webBrowserPageModel.Title);
        }

        [Test]
        public void ToggleSearch_InvertsIsSearchVisible()
        {
            // Arrange
            var initialValue = _webBrowserPageModel.IsSearchVisible;

            // Act
            _webBrowserPageModel.ToggleSearchCommand.Execute(null);

            // Assert
            Assert.AreEqual(!initialValue, _webBrowserPageModel.IsSearchVisible);
        }
    }
}
