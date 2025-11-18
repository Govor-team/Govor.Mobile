using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class SettingsPageModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DeviceSession> sessions = new();

    [ObservableProperty]
    private string userName = "Your name";

    [ObservableProperty]
    private string about = "";

    [ObservableProperty]
    private string avatarIcon = "default_avatar.png";

    private readonly ISessionsService _sessionsService;
    private readonly IDeviceInfoParserService _infoParserService;
    private readonly IUserProfileDonloaderSerivce _profileDonloaderSerivce;
    private readonly IMediaLoaderService _mediaLoaderService;

    public SettingsPageModel(
        ISessionsService userSession,
        IUserProfileDonloaderSerivce userProfile,
        IDeviceInfoParserService infoParserService,
        IMediaLoaderService mediaLoader)

    {
        _sessionsService = userSession;
        _infoParserService = infoParserService;
        _profileDonloaderSerivce = userProfile;
        _mediaLoaderService = mediaLoader;
    }

    public async Task Init()
    {
        await InitSessions();
    }

    private async Task InitSessions()
    {
        var sessionResult = await _sessionsService.GetAllSessionsAsync();

        if (!sessionResult.IsSuccess)
        {
            await AppShell.DisplaySnackbarAsync(sessionResult.ErrorMessage);
            return;
        }

        sessions?.Clear();

        foreach (var session in sessionResult.Value)
        {
            try
            {
                var info = _infoParserService.Parse(session.deviceInfo);
                var icon = GetPlatformIcon(info.Platform);

                sessions?.Add(new DeviceSession
                {
                    CreatedAt = session.createdAt,
                    Icon = icon,
                    DeviceName = info.DeviceName,
                    Id = session.id,
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка парсинга сессии: {session.deviceInfo} - {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task RemoveSessionAsync(DeviceSession session)
    {
        var result = await _sessionsService.CloseSessionAsync(session.Id);

        if (!result.IsSuccess)
        {
            await AppShell.DisplaySnackbarAsync(result.ErrorMessage);
            return;
        }

        Sessions.Remove(session);
    }

    private string GetPlatformIcon(string info)
    {
        if (info.StartsWith("Win", StringComparison.OrdinalIgnoreCase))
            return "windows_icon.png";
        if (info.StartsWith("Android", StringComparison.OrdinalIgnoreCase))
            return "android_icon.png";
        if (info.StartsWith("iOS", StringComparison.OrdinalIgnoreCase))
            return "ios_icon.png";

        return "default_device.png";
    }

    public class DeviceSession
    {
        public string DeviceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Icon { get; set; } = string.Empty;
        public Guid Id { get; set; }
    }
}
