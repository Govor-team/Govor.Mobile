using Govor.Mobile.Models.Results;

namespace Govor.Mobile.Services.Api;

public interface IUserProfileDonloaderSerivce
{
    Task<Result<UserProfile>> GetProfile();
    Task<Result<UserProfile>> GetProfileByUserId(Guid id);
}
