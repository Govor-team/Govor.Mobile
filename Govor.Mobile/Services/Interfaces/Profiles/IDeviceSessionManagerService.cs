namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IDeviceSessionManagerService
{
    Task<bool> CloseSessionAsync(Guid sessionId);
    Task<List<DeviceSession>> LoadSessionsAsync();
}

public class DeviceSession
{
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Icon { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public bool IsCurrent { get; set; }
}