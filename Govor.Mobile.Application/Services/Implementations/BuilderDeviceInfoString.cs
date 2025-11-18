using Govor.Mobile.Application.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Services.Implementations
{
    public class BuilderDeviceInfoString : IBuilderDeviceInfoString
    {
        public string Info => $"{DeviceInfo.Name} on {DeviceInfo.Platform} {DeviceInfo.VersionString}";
    }
}
