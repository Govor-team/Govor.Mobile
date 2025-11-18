using Govor.Mobile.Application.Services.Interfaces.Profiles;

namespace Govor.Mobile.Application.Services.Implementations.Profiles;

public class AvatarStoragePathService : IAvatarStoragePath
{
    public string AvatarsFolder => Path.Combine(FileSystem.AppDataDirectory, "avatars");
}
