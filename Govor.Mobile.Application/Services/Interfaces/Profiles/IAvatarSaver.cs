namespace Govor.Mobile.Application.Services.Interfaces.Profiles;

public interface IAvatarSaver
{
    public Task<string> SaveAvatarAsync(Guid id, string fileName, Stream stream);
    public void DeleteAvatarAsync(string fileName);
}
