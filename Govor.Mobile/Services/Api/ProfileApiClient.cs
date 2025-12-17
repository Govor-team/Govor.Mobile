using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;

namespace Govor.Mobile.Services.Api;

public class ProfileApiClient : IProfileApiClient
{
    private const string path ="/api/profile/";
    private readonly IApiClient _apiClient;

    public ProfileApiClient(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Result<UploadMediaResponse>> UploadAvatar(AvatarUploadRequest request)
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

            content.Add(new StringContent(request.Type.ToString()), "Type"); 
            content.Add(new StringContent(request.MimeType), "MimeType");
            
            var httpResult = await _apiClient.PostMultipartAsync( path + "avatar", content);
            
            if (!httpResult.IsSuccess)
                return Result<UploadMediaResponse>.Failure(httpResult.ErrorMessage);
            
            if (httpResult == null)
                return Result<UploadMediaResponse>.Failure("Failed to deserialize server response");
            
            return Result<UploadMediaResponse>.Success(httpResult.Value);
        }
        catch (Exception ex)
        {
            return Result<UploadMediaResponse>.Failure(ex.Message);
        }
    }

    public async Task<UserProfileDto> DownloadCurrentAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<UserProfileDto>(path + "download/me");

            if (result.IsSuccess)
                return result.Value;

            return null;
        }
        catch(Exception ex)
        {
            return null;
        }
    }

    public async Task<UserProfileDto> DowloadProfileByUserIdAsync(Guid id)
    {
        try
        {
            var result = await _apiClient.GetAsync<UserProfileDto>(path + "download/" + id);

            if (result.IsSuccess)
                return result.Value;

            return null;
        }
        catch
        {
            return null;
        }
    }
}