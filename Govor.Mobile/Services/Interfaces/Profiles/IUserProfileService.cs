using Govor.Mobile.Models.Results;

namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IUserProfileService
{
    Task<UserProfile> GetCurrentProfile();
    Task<UserProfile> GetProfileAsync(Guid userId);
    Task UpdateProfileFromHub(UserProfileDelta profile); 
    event Action<UserProfile>? OnProfileUpdated;
}


public class UserProfileDelta
{
    public Guid UserId { get; set; }

    public string? Description { get; set; } 
    
    public Guid? IconId { get; set; } 
}