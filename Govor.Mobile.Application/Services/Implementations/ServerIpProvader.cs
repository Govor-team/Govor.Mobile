using Govor.Mobile.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Services.Implementations;

internal class ServerIpProvader : IServerIpProvader
{
    public string IP => "https://localhost:7155";
}
