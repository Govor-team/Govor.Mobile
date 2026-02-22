namespace Govor.Mobile.Services.Interfaces;

public interface IUpdateService
{
    event Action<bool>? UpdateAvailabilityChanged;
    bool HasNewUpdate { get; }
    Task<bool> CheckForUpdatesAsync();
    Task UpdateAsync();
}