namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IAvatarSaver
{
    public Task<string> SaveAvatarAsync(Guid id, string fileName, Stream stream);
    public Task DeleteLocalAvatarAsync();
}
