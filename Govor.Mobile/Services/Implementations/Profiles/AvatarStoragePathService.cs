using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class AvatarStoragePathService : IAvatarStoragePath
{
    public string AvatarsFolder => Path.Combine(FileSystem.AppDataDirectory, "avatars");
}
