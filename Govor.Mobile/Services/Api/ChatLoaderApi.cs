using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class ChatLoaderApi : IChatLoaderApi
{
    private readonly IApiClient _apiClient;
    private readonly Govor.Mobile.Services.E2ee.E2eeClient _e2ee;
    
    public ChatLoaderApi(IApiClient apiClient, Govor.Mobile.Services.E2ee.E2eeClient e2ee)
    {
        _apiClient = apiClient;
        _e2ee = e2ee;
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
            await DecodeAsync(result.Value, groupId, 1);
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
            await DecodeAsync(result.Value, userId, 0);
            return Result<List<MessageResponse>>.Success(
                result.Value ?? new List<MessageResponse>()
            );

        }
        else
        {
            return Result<List<MessageResponse>>.Failure(result.ErrorMessage);
        }
    }
    private async Task DecodeAsync(List<MessageResponse>? messages, Guid conversation, int type)
    {
        if (messages is null) return;
        foreach (var message in messages)
        {
            try
            {
                if (message.RecipientId != conversation || (int)message.RecipientType != type)
                    throw new System.Security.Cryptography.CryptographicException("Invalid history context.");
                message.EncryptedContent = await _e2ee.DecryptAsync(message.EncryptedContent,
                    message.Id, conversation, type, message.SenderId, message.ReplyToMessageId, message.IsEdited);
            }
            catch
            {
                message.E2eeDecryptionFailed = true;
                message.EncryptedContent = "[Сообщение E2EE недоступно: ключ не подтверждён, отсутствует или проверка не пройдена]";
                message.MediaAttachments.Clear();
            }
        }
    }
}
