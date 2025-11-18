namespace Govor.Mobile.Application.Services.Interfaces.Profiles;

public interface IProfileCacheService
{
    Task<Models.LocalUserProfile> GetCurrentAsync();
    Task<Models.LocalUserProfile> GetProfileByUserId(Guid userId);
    Task<Models.LocalUserProfile> RefreshAsync();
}
