namespace Govor.Mobile.Services.Interfaces;

public interface IDeviceInfoParserService
{
    public DeviceInfoData Parse(string info);
}

public class DeviceInfoData
{
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
