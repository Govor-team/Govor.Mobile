using NUnit.Framework;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;
using System;

namespace Govor.Mobile.Tests.Services.Implementations
{
    [TestFixture]
    public class DeviceInfoParserServiceTests
    {
        private DeviceInfoParserService _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new DeviceInfoParserService();
        }

        [TestCase("My Phone on Android 11")]
        [TestCase("   Another Device   on   iOS   15.2.1   ")]
        [TestCase("Desktop-PC on Windows 10.0.19042")]
        public void Parse_WithValidString_ReturnsCorrectData(string info)
        {
            // Arrange
            var expectedDeviceName = info.Split(new[] { " on " }, StringSplitOptions.None)[0].Trim();
            var platformAndVersion = info.Split(new[] { " on " }, StringSplitOptions.None)[1].Trim();
            var expectedPlatform = platformAndVersion.Split(' ')[0];
            var expectedVersion = platformAndVersion.Substring(expectedPlatform.Length).Trim();


            // Act
            var result = _parser.Parse(info);

            // Assert
            Assert.AreEqual(expectedDeviceName, result.DeviceName);
            Assert.AreEqual(expectedPlatform, result.Platform);
            Assert.AreEqual(expectedVersion, result.Version);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void Parse_WithNullOrEmptyString_ThrowsArgumentException(string info)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _parser.Parse(info));
        }

        [TestCase("Invalid format")]
        [TestCase("Device onPlatformVersion")]
        [TestCase("Device on Platform")]
        public void Parse_WithInvalidFormat_ThrowsFormatException(string info)
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => _parser.Parse(info));
        }
    }
}
