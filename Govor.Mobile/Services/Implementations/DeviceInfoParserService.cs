using Govor.Mobile.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Govor.Mobile.Services.Implementations;

public class DeviceInfoParserService : IDeviceInfoParserService
{
    // Формат: "Имя устройства on Платформа Версия"
    private static readonly Regex _regex = new(@"^(.*?)\s+on\s+([^\s]+)\s+([\w\.]+)$", RegexOptions.Compiled);

    public DeviceInfoData Parse(string info)
    {
        if (string.IsNullOrWhiteSpace(info))
            throw new ArgumentException("Info string cannot be null or empty.", nameof(info));

        var match = _regex.Match(info);
        if (!match.Success)
            throw new FormatException($"Cannot parse device info string: '{info}'");

        return new DeviceInfoData
        {
            DeviceName = match.Groups[1].Value.Trim(),
            Platform = match.Groups[2].Value.Trim(),
            Version = match.Groups[3].Value.Trim()
        };
    }
}
