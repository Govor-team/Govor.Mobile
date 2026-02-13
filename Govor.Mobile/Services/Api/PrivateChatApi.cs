using Govor.Mobile.Services.Api.Base;
using Markdig.Extensions.TaskLists;

namespace Govor.Mobile.Services.Api;

public class PrivateChatApi : IPrivateChatApi
{
    private readonly IApiClient _apiClient;
    private readonly Dictionary<Guid, Guid> _privateChats = new();
    
    public PrivateChatApi(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Result<Guid>> GetChatByFriendId(Guid friendId)
    {
        if (_privateChats.ContainsKey(friendId))
            return Result<Guid>.Success(_privateChats[friendId]);
        
        var path = $"/api/user/{friendId}/private-chat";
        var result = await _apiClient.GetAsync<Guid>(path, authenticated: true);

        if (result.IsSuccess)
        {
            _privateChats[friendId] = result.Value;
            return Result<Guid>.Success(_privateChats[friendId]);
        }
        else
        {
            return Result<Guid>.Failure(result.ErrorMessage);
        }
    }
}