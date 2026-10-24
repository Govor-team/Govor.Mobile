using Govor.Mobile.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Govor.Mobile.Services.Implementations;

public class DeviceInfoParserService : IDeviceInfoParserService
{
    // Descriptions from older or third-party clients may be arbitrary text.
    // Parse the current client's format when possible, but keep every session visible.
    private static readonly Regex _regex = new(
        @"^(?<name>.+)\s+on\s+(?<platform>\S+)\s+(?<version>\S+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public DeviceInfoData Parse(string? info)
    {
        if (string.IsNullOrWhiteSpace(info))
            return new DeviceInfoData { DeviceName = "Неизвестное устройство" };

        var description = info.Trim();
        var match = _regex.Match(description);
        if (!match.Success)
            return new DeviceInfoData { DeviceName = description };

        return new DeviceInfoData
        {
            DeviceName = match.Groups["name"].Value.Trim(),
            Platform = match.Groups["platform"].Value,
            Version = match.Groups["version"].Value
        };
    }
}
