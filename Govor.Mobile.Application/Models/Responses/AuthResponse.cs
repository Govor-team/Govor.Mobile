using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Application.Models.Responses
{
    public record AuthResponse(string refreshToken, string accessToken);
}
