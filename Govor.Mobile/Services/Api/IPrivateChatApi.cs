namespace Govor.Mobile.Services.Api;

public interface IPrivateChatApi
{
    public Task<Result<Guid>> GetChatByFriendId(Guid friendId);
}