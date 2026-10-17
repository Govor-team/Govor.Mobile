using System.IdentityModel.Tokens.Jwt;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Implementations.Notification;

// Only successful registrations are cached, for this process and this JWT session.
public sealed class PushTokenRegistration(IJwtProviderService session, IPushTokenService api)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private (Guid User, string Sid, string Token)? _registered;

    public async Task<bool> RegisterAsync(string token, string platform)
    {
        if (string.IsNullOrWhiteSpace(token) || !session.HasRefreshToken) return false;
        await _gate.WaitAsync();
        try
        {
            if (!session.HasRefreshToken || session.CurrentUserId is not Guid user) return false;
            var accessToken = await session.GetAccessTokenAsync();
            var sid = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims
                .FirstOrDefault(c => c.Type == "sid")?.Value;
            if (string.IsNullOrWhiteSpace(sid)) return false;
            var registration = (user, sid, token);
            if (_registered == registration) return true;
            var result = await api.PushToken(token, platform);
            if (!result.IsSuccess || !session.HasRefreshToken || session.CurrentUserId != user) return false;
            _registered = registration;
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> UnregisterAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _registered = null;
            return !session.HasRefreshToken || (await api.DeleteTokenAsync()).IsSuccess;
        }
        finally { _gate.Release(); }
    }
}
