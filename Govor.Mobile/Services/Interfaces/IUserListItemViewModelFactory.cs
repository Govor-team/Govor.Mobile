using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces;

public interface IUserListItemViewModelFactory
{
    UserListItemViewModel Create(UserProfileDto profile);
    Task<UserListItemViewModel> CreateAsync(Guid userId);
    Task<UserListItemViewModel> CreateAsync(UserDto user);
}