using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Hubs;

public class ProfileHubListener
{
    private readonly IProfileHubService _hubService;
    private readonly IUserProfileService _profileService;

    public ProfileHubListener(IProfileHubService hubService, IUserProfileService profileService)
    {
        _hubService = hubService;
        _profileService = profileService;

        _hubService.OnDescriptionUpdated += async (userId, payload) =>
        {
            await _profileService.UpdateProfileFromHub(
                new UserProfileDelta { UserId = userId, Description = payload.Description });
        };
        
        _hubService.OnAvatarUpdated += async (iconId, payload) =>
        {
            await _profileService.UpdateProfileFromHub(
                new UserProfileDelta(){UserId = payload.UserId, IconId = iconId}); 
        };
    }
}