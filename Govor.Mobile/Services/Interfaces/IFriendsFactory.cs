using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces;

public interface IFriendsFactory
{
    Task<UserListItemViewModel> CreateAsync(UserProfileDto profile);
}