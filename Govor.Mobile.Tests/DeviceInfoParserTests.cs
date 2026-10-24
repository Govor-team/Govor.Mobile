using Govor.Mobile.Services.Implementations;

namespace Govor.Mobile.Tests;

[TestFixture]
public class DeviceInfoParserTests
{
    private readonly DeviceInfoParserService _parser = new();

    [TestCase("My phone on Android 14", "My phone", "Android", "14")]
    [TestCase("  Home PC on WinUI 10.0.22631.0  ", "Home PC", "WinUI", "10.0.22631.0")]
    [TestCase("Laptop on desk on Windows 11.0", "Laptop on desk", "Windows", "11.0")]
    [TestCase("Pixel on Android 16-beta.1+build.42", "Pixel", "Android", "16-beta.1+build.42")]
    [TestCase("iPhone on iOS 18.5", "iPhone", "iOS", "18.5")]
    public void ParsesStructuredDescriptions(string description, string name, string platform, string version)
    {
        var result = _parser.Parse(description);
        Assert.Multiple(() =>
        {
            Assert.That(result.DeviceName, Is.EqualTo(name));
            Assert.That(result.Platform, Is.EqualTo(platform));
            Assert.That(result.Version, Is.EqualTo(version));
        });
    }

    [TestCase("Codex groups diagnostics")]
    [TestCase("  Legacy desktop client  ")]
    [TestCase("Phone on Android")]
    [TestCase("on Android 14")]
    public void UnstructuredDescriptionStaysVisibleWithGenericIcon(string description)
    {
        var result = _parser.Parse(description);
        Assert.Multiple(() =>
        {
            Assert.That(result.DeviceName, Is.EqualTo(description.Trim()));
            Assert.That(result.Platform, Is.Empty);
            Assert.That(result.Version, Is.Empty);
            Assert.That(new PlatformIconService().GetPlatformIcon(result.Platform), Is.EqualTo("default_device.png"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t\r\n ")]
    public void MissingDescriptionHasSafeDisplayName(string? description)
    {
        var result = _parser.Parse(description);
        Assert.Multiple(() =>
        {
            Assert.That(result.DeviceName, Is.EqualTo("Неизвестное устройство"));
            Assert.That(result.Platform, Is.Empty);
            Assert.That(result.Version, Is.Empty);
        });
    }
}
