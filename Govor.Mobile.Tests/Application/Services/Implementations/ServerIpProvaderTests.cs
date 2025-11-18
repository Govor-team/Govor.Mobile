using NUnit.Framework;
using Govor.Mobile.Application.Services.Implementations;

namespace Govor.Mobile.Tests.Application.Services.Implementations
{
    [TestFixture]
    public class ServerIpProvaderTests
    {
        [Test]
        public void IP_ReturnsCorrectHardcodedValue()
        {
            // Arrange
            var provader = new ServerIpProvader();
            var expectedIp = "https://localhost:7155";

            // Act
            var actualIp = provader.IP;

            // Assert
            Assert.AreEqual(expectedIp, actualIp);
        }
    }
}
