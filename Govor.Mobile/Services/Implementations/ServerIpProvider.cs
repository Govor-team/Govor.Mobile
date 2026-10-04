using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

internal class ServerIpProvider : IServerIpProvider
{
#if RELEASE
    public string IP => "https://stalcker2288969-govor-7640.twc1.net";//"https://govor-team-govor-870e.twc1.net";
#elif DEBUG
    public string IP => "http://10.0.2.2:7155"; //"http://10.8.0.5:5000";
#endif
}
// command pas: {key}

//DeviceInfo.Platform == DevicePlatform.Android ?
//"http://10.0.2.2:7155" : "http://localhost:7155";
//public string IP => "https://govor-team-govor-870e.twc1.net";