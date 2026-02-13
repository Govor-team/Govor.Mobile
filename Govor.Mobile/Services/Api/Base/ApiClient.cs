using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api.Base;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IJwtProviderService _jwtProvider;
    private readonly ILogger<ApiClient> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(
        IServerIpProvader ipProvader,
        IJwtProviderService jwtProvider,
        ILogger<ApiClient> logger)
    {
        _httpClient = new HttpClient();
        _jwtProvider = jwtProvider;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(ipProvader.IP);
    }

    // Универсальный метод, с обработкой 401
    private async Task<HttpResult<T>> SendWithAuthRetryAsync<T>(
        Func<HttpRequestMessage> createRequest, bool authenticated = true)
    {
        async Task<HttpResponseMessage> SendAuthorizedAsync()
        {
            using var request = createRequest();

            if (authenticated)
            {
                var token = await _jwtProvider.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await _httpClient.SendAsync(request);
        }

        var response = await SendAuthorizedAsync();

        // Retry once if 401
        if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated)
        {
            _logger.LogInformation("Access token expired. Retrying request...");
            response = await SendAuthorizedAsync();
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Request failed: {Error}", error);
            return new HttpResult<T>(error, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(json))
        {
            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return HttpResult<T>.Success(result!);
        }
        else
        {
            return HttpResult<T>.Success(default);
        }
    }

    

    public async Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, endpoint), authenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending GET request.");
            return HttpResult<T>.FromException(ex);
        }
    }


    // POST
    public async Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() =>
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                return new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            }, authenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending POST request.");
            return HttpResult<T>.FromException(ex);
        }
    }


    // PUT
    public async Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() =>
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                return  new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content };
            }, authenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending PUT request.");
            return HttpResult<T>.FromException(ex);
        }
    }

    // DELETE
    public async Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true)
    {
        try
        {
            var result = await SendWithAuthRetryAsync<string>(() =>
                new HttpRequestMessage(HttpMethod.Delete, endpoint), authenticated);

            return result.IsSuccess
                ? HttpResult<bool>.Success(true)
                : new HttpResult<bool>(false, result.Value ?? "", result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending DELETE request.");
            return HttpResult<bool>.FromException(ex);
        }
    }


    // POST multipart/form-data (для загрузки медиа)
    public async Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(
        string endpoint, MultipartFormDataContent form, bool authenticated = true)
    {
        try
        {
            return await SendWithAuthRetryAsync<UploadMediaResponse>(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = form
                };
                return request;
            }, authenticated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending multipart POST request.");
            return HttpResult<UploadMediaResponse>.FromException(ex);
        }
    }


    // GET (stream download, для скачивания медиа)
    public async Task<HttpResult<Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true)
    {
        try
        {
            async Task<HttpResponseMessage> SendAuthorizedAsync(Func<HttpRequestMessage> createRequest)
            {
                using var request = createRequest();

                if (authenticated)
                {
                    var token = await _jwtProvider.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                return await _httpClient.SendAsync(request);
            }
            
            var response = await SendAuthorizedAsync( () => new HttpRequestMessage(HttpMethod.Get, endpoint));

            if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated)
            {
                _logger.LogInformation("Access token expired. Retrying request...");
                response = await SendAuthorizedAsync( () => new HttpRequestMessage(HttpMethod.Get, endpoint));
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new HttpResult<Utilities.FileResult>(error, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();

            // Получаем имя файла из заголовка Content-Disposition
            var contentDisposition = response.Content.Headers.ContentDisposition;
            string fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName ?? "downloaded_file";

            // Получаем MIME-тип
            string? mimeType = response.Content.Headers.ContentType?.MediaType;

            return HttpResult<Utilities.FileResult>.Success(new Utilities.FileResult(stream, fileName, mimeType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while downloading stream.");
            return HttpResult<Utilities.FileResult>.FromException(ex);
        }
    }
}
