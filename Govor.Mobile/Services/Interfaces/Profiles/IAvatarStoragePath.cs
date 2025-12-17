using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IAvatarStoragePath
{
    public string UserAvatarsFolder { get; }
    
    string GetAvatarFilePath(Guid id, string fileExtension);
}
