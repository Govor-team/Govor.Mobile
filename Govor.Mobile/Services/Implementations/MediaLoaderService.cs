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

    public Task<Result<UploadMediaResponse>> Upload(UploadMediaRequest request)
    {
        throw new NotImplementedException();
    }
}
