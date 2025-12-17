using System.Text.Json;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

class MediaLoaderService : IMediaLoaderService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<MediaLoaderService> _logger;

    public MediaLoaderService(IApiClient apiClient, ILogger<MediaLoaderService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<Result<DownloadMediaResponse>> Download(Guid id)
    {
        try
        {
            var result = await _apiClient.GetFileStreamAsync($"api/media/download/{id}");

            if (!result.IsSuccess)
                return Result<DownloadMediaResponse>.Failure(result.ErrorMessage);
            
            var response = result.Value;


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

            if (!httpResult.IsSuccess)
                return Result<UploadMediaResponse>.Failure(httpResult.ErrorMessage);
            
            if (httpResult == null)
                return Result<UploadMediaResponse>.Failure("Failed to deserialize server response");

            return Result<UploadMediaResponse>.Success(httpResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return Result<UploadMediaResponse>.Failure(ex.Message);
        }
    }
}
