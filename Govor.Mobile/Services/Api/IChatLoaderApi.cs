using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api;

public interface IChatLoaderApi
{
    Task<Result<List<MessageResponse>>> GetGroupMessages(Guid groupId, MessageQuery query);
    Task<Result<List<MessageResponse>>> GetUserMessages(Guid userId, MessageQuery query);
}