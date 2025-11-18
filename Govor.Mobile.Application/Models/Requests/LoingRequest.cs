using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Models.Requests
{
    public record LoingRequest(string name, string password, string deviceInfo);
}
