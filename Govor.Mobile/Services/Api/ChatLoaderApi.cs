using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class ChatLoaderApi : IChatLoaderApi
{
    private readonly IApiClient _apiClient;
    
    public ChatLoaderApi(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Result<List<MessageResponse>>> GetGroupMessages(Guid groupId, MessageQuery query)
    {
        var path = $"/api/groups/{groupId}/messages";

        // Добавляем Query-параметры (?Before=20&After=2...)
        var queryString = $"?Before={query.Before}&After={query.After}";
        if (query.StartMessageId.HasValue)
        {
            queryString += $"&StartMessageId={query.StartMessageId.Value}";
        }

        var fullUrl = path + queryString;

        var result = await _apiClient.GetAsync<List<MessageResponse>>(fullUrl, authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<MessageResponse>>.Success(
                result.Value ?? new List<MessageResponse>()
            );

        }
        else
        {
            return Result<List<MessageResponse>>.Failure(result.ErrorMessage);
        }
    }

    public async Task<Result<List<MessageResponse>>> GetUserMessages(Guid userId, MessageQuery query)
    {
        var path = $"/api/user/{userId}/messages";

        var queryString = $"?Before={query.Before}&After={query.After}";
        if (query.StartMessageId.HasValue)
        {
            queryString += $"&StartMessageId={query.StartMessageId.Value}";
        }

        var fullUrl = path + queryString;

        var result = await _apiClient.GetAsync<List<MessageResponse>>(fullUrl, authenticated: true);

        if (result.IsSuccess)
        {
            return Result<List<MessageResponse>>.Success(
                result.Value ?? new List<MessageResponse>()
            );

        }
        else
        {
            return Result<List<MessageResponse>>.Failure(result.ErrorMessage);
        }
    }
}