using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class AvatarStoragePathService : IAvatarStoragePath
{
    public string UserAvatarsFolder => Path.Combine(FileSystem.AppDataDirectory, "user_avatars");
    
    public string GetAvatarFilePath(Guid id, string fileExtension)
    {
        var ext = fileExtension.StartsWith(".") ? fileExtension : $".{fileExtension}";
        
        return Path.Combine(UserAvatarsFolder, $"{id}{ext}");
    }
}
