using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Models.Requests
{
    public record RegisterRequest(string name, string password, string inviteLink, string deviceInfo);
}
