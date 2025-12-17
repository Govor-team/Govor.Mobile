namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface ICurrentUserAvatarService
{
    Task<ImageSource> LoadAvatarAsync(Guid avatarId);
    Task<Guid?> PickAndUploadNewAvatarAsync();
}