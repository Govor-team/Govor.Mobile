using Govor.Mobile.Models.DTO;

namespace Govor.Mobile.Services.Api;

public interface IPrivateChatApi
{
    public Task<Result<Guid>> GetChatByFriendId(Guid friendId);
    public Task<Result<IEnumerable<PrivateChatDto>>> GetPrivateChats();
}