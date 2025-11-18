using Govor.Mobile.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Implementations
{
    public class BuilderDeviceInfoString : IBuilderDeviceInfoString
    {
        public string Info => $"{DeviceInfo.Name} on {DeviceInfo.Platform} {DeviceInfo.VersionString}";
    }
}
