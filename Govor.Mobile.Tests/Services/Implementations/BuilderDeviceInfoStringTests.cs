using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
using Moq;
using Microsoft.Maui.Devices;

namespace Govor.Mobile.Tests.Services.Implementations
{
    [TestFixture]
    public class BuilderDeviceInfoStringTests
    {
        [Test]
        public void Info_ReturnsCorrectlyFormattedString()
        {
            // Arrange

            // We can't directly mock static properties of DeviceInfo.
            // We'll test the service assuming DeviceInfo provides some values.
            // In a real-world scenario with more complex logic, we might need
            // to wrap DeviceInfo in an interface to make it mockable.

            var builder = new BuilderDeviceInfoString();
            var expectedInfo = $"{DeviceInfo.Name} on {DeviceInfo.Platform} {DeviceInfo.VersionString}";

            // Act
            var actualInfo = builder.Info;

            // Assert
            Assert.AreEqual(expectedInfo, actualInfo);
        }
    }
}
