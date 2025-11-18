using Govor.Mobile.Application.Models.Results;

namespace Govor.Mobile.Application.Services.Api;

public interface IUserProfileDonloaderSerivce
{
    Task<Result<UserProfile>> GetProfile();
    Task<Result<UserProfile>> GetProfileByUserId(Guid id);
}
