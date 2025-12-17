using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class PlatformIconService : IPlatformIconService
{
    public string GetPlatformIcon(string platformInfo)
    {
        if (string.IsNullOrWhiteSpace(platformInfo))
            return "default_device.png";

        if (platformInfo.StartsWith("Win", StringComparison.OrdinalIgnoreCase))
            return "windows_icon.png";

        if (platformInfo.StartsWith("Android", StringComparison.OrdinalIgnoreCase))
            return "android_icon.png";

        if (platformInfo.StartsWith("iOS", StringComparison.OrdinalIgnoreCase))
            return "ios_icon.png";

        return "default_device.png";
    }
}