using Govor.Mobile.Models.DTO;
using Govor.Mobile.Utilities;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Api;

public class PrivateChatApi : IPrivateChatApi
{
    private readonly IApiClient _apiClient;
    private readonly Dictionary<(Guid Account, Guid Friend), Guid> _privateChats = new();
    private readonly IJwtProviderService _session;

    public PrivateChatApi(IApiClient apiClient, IJwtProviderService session)
    {
        _apiClient = apiClient; _session = session;
    }

    public async Task<Result<Guid>> GetChatByFriendId(Guid friendId)
    {
        if (_session.CurrentUserId is not Guid account) return Result<Guid>.Failure("Нет активной сессии.");
        var key = (account, friendId);
        if (_privateChats.TryGetValue(key, out var cached)) return Result<Guid>.Success(cached);
        
        var path = $"/api/user/{friendId}/private-chat";
        var result = await _apiClient.GetAsync<Guid>(path, authenticated: true);

        if (_session.CurrentUserId != account) return Result<Guid>.Failure("Аккаунт изменился. Откройте чат заново.");
        if (result.IsSuccess)
        {
            _privateChats[key] = result.Value;
            return Result<Guid>.Success(_privateChats[key]);
        }
        else
        {
            return Result<Guid>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<IEnumerable<PrivateChatDto>>> GetPrivateChats()
    {
        var path = $"/api/user/private-chats";
        var result = await _apiClient.GetAsync<IEnumerable<PrivateChatDto>>(path, authenticated: true);

        if (result.IsSuccess)
        {
            return Result<IEnumerable<PrivateChatDto>>.Success(result.Value);
        }
        else
        {
            return Result<IEnumerable<PrivateChatDto>>.Failure(result.ErrorMessage);
        }
    }
}