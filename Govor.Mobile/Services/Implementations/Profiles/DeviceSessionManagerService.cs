using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class DeviceSessionManagerService : IDeviceSessionManagerService
{
    private readonly IDeviceInfoParserService _infoParserService;
    private readonly IPlatformIconService _iconService;
    private readonly Govor.Mobile.Services.Interfaces.JwtServices.IJwtProviderService _jwt;
    private readonly ISessionsService _sessionsService;

    public DeviceSessionManagerService(
        IDeviceInfoParserService infoParserService,
        IPlatformIconService iconService,
        Govor.Mobile.Services.Interfaces.JwtServices.IJwtProviderService jwt,
        ISessionsService sessionsService)
    {
        _infoParserService = infoParserService;
        _iconService = iconService;
        _jwt = jwt;
        _sessionsService = sessionsService;
    }

    public async Task<bool> CloseSessionAsync(Guid sessionId)
    {
        var result = await _sessionsService.CloseSessionAsync(sessionId);

        if (!result.IsSuccess)
        {
            await AppShell.DisplayException(result.ErrorMessage);
            return false;
        }

        return true;
    }

    public async Task<bool> IsCurrentSessionAsync(Guid sessionId) =>
        sessionId == SessionIdentity.ReadSessionId(await _jwt.GetAccessTokenAsync());

    public async Task<List<DeviceSession>> LoadSessionsAsync()
    {
        List<DeviceSession> sessions = new List<DeviceSession>();
        var sessionResult = await _sessionsService.GetAllSessionsAsync();

        if (!sessionResult.IsSuccess)
        {
            await AppShell.DisplayException(sessionResult.ErrorMessage);
            return null;
        }

        var newSessions = sessionResult.Value
            .OrderByDescending(s => s.createdAt).ToArray();
        var currentSession = SessionIdentity.ReadSessionId(await _jwt.GetAccessTokenAsync());
        
        foreach (var session in newSessions)
        {
            try
            {
                var info = _infoParserService.Parse(session.deviceInfo);
                var icon = _iconService.GetPlatformIcon(info.Platform);
                
                var isCurrent = currentSession == session.id;

                sessions?.Add(new DeviceSession
                {
                    CreatedAt = session.createdAt,
                    Icon = icon,
                    DeviceName = info.DeviceName,
                    Id = session.id,
                    IsCurrent = isCurrent,  
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга сессии: {session.deviceInfo} - {ex.Message}");
            }
        }
        
        return sessions;
    }
}
