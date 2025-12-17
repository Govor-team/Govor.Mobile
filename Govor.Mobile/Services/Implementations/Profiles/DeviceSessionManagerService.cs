using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class DeviceSessionManagerService : IDeviceSessionManagerService
{
    private readonly IDeviceInfoParserService _infoParserService;
    private readonly IPlatformIconService _iconService;
    private readonly IBuilderDeviceInfoString _deviceInfoBuilder;
    private readonly ISessionsService _sessionsService;

    public DeviceSessionManagerService(
        IDeviceInfoParserService infoParserService,
        IPlatformIconService iconService,
        IBuilderDeviceInfoString deviceInfoBuilder,
        ISessionsService sessionsService)
    {
        _infoParserService = infoParserService;
        _iconService = iconService;
        _deviceInfoBuilder = deviceInfoBuilder;
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
        
        foreach (var session in newSessions)
        {
            try
            {
                var info = _infoParserService.Parse(session.deviceInfo);
                var icon = _iconService.GetPlatformIcon(info.Platform);
                
                var isCurrent = _deviceInfoBuilder.Info == session.deviceInfo;

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