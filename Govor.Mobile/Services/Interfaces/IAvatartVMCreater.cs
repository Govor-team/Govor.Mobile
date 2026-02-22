using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces;

public interface IAvatartVMCreater
{
    Task<AvatarViewModel> CreateAvatar(Guid userId);
}