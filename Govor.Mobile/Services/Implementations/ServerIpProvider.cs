using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

internal class ServerIpProvider : IServerIpProvider
{
    public string IP => DeviceInfo.Platform == DevicePlatform.Android ?
        "http://10.0.2.2:7155" : "http://localhost:7155";
}
