using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Models.Responses
{
    public record RefreshResponse(string accessToken, string refreshToken);
}
