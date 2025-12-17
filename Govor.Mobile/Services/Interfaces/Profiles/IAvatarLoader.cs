namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IAvatarLoader
{
    Task<ImageSource?> LoadLocalAvatarAsync(Guid id);
}