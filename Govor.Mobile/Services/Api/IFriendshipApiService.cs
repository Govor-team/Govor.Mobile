using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api;

public interface IFriendshipApiService
{
    Task<Result<List<UserDto>>> Search(string query);
    Task<Result<List<UserDto>>> GetFriends();
}