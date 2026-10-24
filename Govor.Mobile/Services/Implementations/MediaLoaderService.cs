using System.Text.Json;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Govor.Mobile.Services.E2ee;
using Govor.Security;

namespace Govor.Mobile.Services.Implementations;

class MediaLoaderService : IMediaLoaderService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<MediaLoaderService> _logger;
    private readonly E2eeClient _e2ee;

    public MediaLoaderService(IApiClient apiClient, ILogger<MediaLoaderService> logger, E2eeClient e2ee)
    {
        _apiClient = apiClient;
        _logger = logger;
        _e2ee = e2ee;
    }

    public async Task<Result<DownloadMediaResponse>> Download(Guid id, bool encryptedOnly = false)
    {
        try
        {
            var account = encryptedOnly ? await _e2ee.GetOwnKeyAsync() : null;
            var result = await _apiClient.GetFileStreamAsync($"api/media/download/{id}");

            if (!result.IsSuccess)
                return Result<DownloadMediaResponse>.Failure(result.ErrorMessage);
            
            var response = result.Value;
            if (encryptedOnly && response.MimeType != E2eeProtocol.EncryptedMimeType)
            {
                await response.Stream.DisposeAsync();
                throw new System.Security.Cryptography.CryptographicException("Ожидалось зашифрованное вложение.");
            }
            if (response.MimeType == E2eeProtocol.EncryptedMimeType)
            {
                var secret = await _e2ee.GetAttachmentAsync(id)
                    ?? throw new System.Security.Cryptography.CryptographicException("Не найден ключ вложения E2EE.");
                await using var source = response.Stream;
                var ciphertext = await ReadBoundedAsync(source, 19_000_028);
                var plaintext = E2eeProtocol.DecryptAttachment(ciphertext, secret.Key);
                if (account is not null)
                {
                    var current = await _e2ee.GetOwnKeyAsync();
                    if (current.UserId != account.UserId || current.SessionId != account.SessionId)
                        throw new UnauthorizedAccessException("Аккаунт изменился.");
                }
                return Result<DownloadMediaResponse>.Success(new()
                {
                    FileStream = new MemoryStream(plaintext, writable: false),
                    FileName = Path.GetFileName(secret.FileName), MimeType = secret.MimeType
                });
            }
            return Result<DownloadMediaResponse>.Success(new DownloadMediaResponse() 
            {   
                FileStream = response.Stream,
                FileName = response.FileName,
                MimeType = response.MimeType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Media download failed");
            return Result<DownloadMediaResponse>.Failure("Internal error during download.");
        }
    }

    public async Task<Result<UploadMediaResponse>> Upload(UploadMediaRequest request)
    {
        if (request.FileStream == null || string.IsNullOrWhiteSpace(request.FileName))
            return Result<UploadMediaResponse>.Failure("File stream or file name is null");

        try
        {
            AttachmentSecret? attachment = null;
            var isMessage = request.OwnerType == Govor.Mobile.Models.Requests.MediaOwnerType.Message;
            var account = isMessage ? await _e2ee.GetOwnKeyAsync() : null;
            if (isMessage)
            {
                var plaintext = await ReadBoundedAsync(request.FileStream, 19_000_000);
                var encrypted = E2eeProtocol.EncryptAttachment(plaintext);
                attachment = new AttachmentSecret(Guid.Empty, encrypted.Key, Path.GetFileName(request.FileName), request.MimeType);
                request = new UploadMediaRequest
                {
                    FileStream = new MemoryStream(encrypted.Data, writable: false), FileName = Guid.NewGuid().ToString("N") + ".bin",
                    MimeType = E2eeProtocol.EncryptedMimeType, Type = MediaType.File,
                    OwnerType = Govor.Mobile.Models.Requests.MediaOwnerType.Message, EncryptedKey = string.Empty
                };
            }
            using var content = new MultipartFormDataContent();

            // Добавляем файл
            var fileContent = new StreamContent(request.FileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.MimeType);

            content.Add(fileContent, "FromFile", request.FileName);

            // Добавляем остальные поля
            content.Add(new StringContent(request.Type.ToString()), "Type");
            content.Add(new StringContent(request.MimeType), "MimeType");
            content.Add(new StringContent(request.EncryptedKey), "EncryptedKey");
            content.Add(new StringContent(request.OwnerType.ToString()), "OwnerType");

            // Отправляем
            var httpResult = await _apiClient.PostMultipartAsync("api/media/upload", content);

            if (httpResult == null || httpResult.IsSuccess && (httpResult.Value is null || httpResult.Value.MediaId == Guid.Empty))
                return Result<UploadMediaResponse>.Failure("Failed to deserialize server response");

            if (!httpResult.IsSuccess)
                return Result<UploadMediaResponse>.Failure(httpResult.ErrorMessage);

            if (attachment is not null)
            {
                var current = await _e2ee.GetOwnKeyAsync();
                if (current.UserId != account!.UserId || current.SessionId != account.SessionId)
                    throw new UnauthorizedAccessException("Аккаунт изменился.");
                await _e2ee.RememberAttachmentAsync(attachment with { MediaId = httpResult.Value.MediaId });
            }

            return Result<UploadMediaResponse>.Success(httpResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return Result<UploadMediaResponse>.Failure(ex.Message);
        }
    }
    private static async Task<byte[]> ReadBoundedAsync(Stream source, int maximum)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk)) != 0)
        {
            if (buffer.Length + read > maximum) throw new InvalidDataException("Превышен размер вложения E2EE.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
