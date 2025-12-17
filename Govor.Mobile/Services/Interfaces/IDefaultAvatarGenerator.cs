using Govor.Mobile.Models;

namespace Govor.Mobile.Services.Interfaces;

public interface IDefaultAvatarGenerator
{
    AvatarModel GenerateAvatar(string userName);
}