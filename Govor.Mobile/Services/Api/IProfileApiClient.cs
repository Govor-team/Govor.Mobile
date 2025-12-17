using Govor.Mobile.Models;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api;

public interface IProfileApiClient
{
    Task<Result<UploadMediaResponse>> UploadAvatar(AvatarUploadRequest request);
    Task<UserProfileDto> DownloadCurrentAsync();
    Task<UserProfileDto> DowloadProfileByUserIdAsync(Guid id);
}