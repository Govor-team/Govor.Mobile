using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

internal class ServerIpProvader : IServerIpProvader
{
#if WINDOWS
    public string IP => "http://localhost:8080";//10.0.2.2 http://localhost:5041 192.168.1.107
#elif ANDROID
    public string IP => "http://10.0.2.2:8080";//10.0.2.2 http://localhost:5041 192.168.1.107 
#else
    public string IP => "http://localhost:8080";
#endif
}
