using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
        Func<Task<HttpResponseMessage>> sendRequest)
    {
        async Task<HttpResponseMessage> SendAuthorizedAsync()
        {
            var token = _jwtProvider.AccessToken;

            if (!string.IsNullOrEmpty(token))
            {
                // Удаляем старый заголовок, если был
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }

            return await sendRequest();
        }

        var response = await SendAuthorizedAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Access token expired. Trying to refresh JWT...");

            var refresh = await _jwtProvider.RefreshAsync();
            if (!refresh.IsSuccess)
            {
                _logger.LogError("Token refresh failed.");
                await _jwtProvider.ClearAsync();
                return new HttpResult<T>(HttpStatusCode.Unauthorized);
            }

            _logger.LogInformation("JWT refreshed successfully. Retrying request...");
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


    // Вспомогательный метод: добавляет JWT и выполняет запрос
    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> sendRequest)
    {
        if (!string.IsNullOrWhiteSpace(_jwtProvider.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _jwtProvider.AccessToken);
        }

        return await sendRequest();
    }

    public async Task<HttpResult<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() => _httpClient.GetAsync(endpoint));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending GET request.");
            return HttpResult<T>.FromException(ex);
        }
    }

    // POST
    public async Task<HttpResult<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() =>
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                return _httpClient.PostAsync(endpoint, content);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending POST request.");
            return HttpResult<T>.FromException(ex);
        }
    }

    // PUT
    public async Task<HttpResult<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            return await SendWithAuthRetryAsync<T>(() =>
            {
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                return _httpClient.PutAsync(endpoint, content);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending PUT request.");
            return HttpResult<T>.FromException(ex);
        }
    }

    // DELETE
    public async Task<HttpResult<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            var result = await SendWithAuthRetryAsync<string>(() => _httpClient.DeleteAsync(endpoint));
            return result.IsSuccess ? HttpResult<bool>.Success(true) : new HttpResult<bool>(false, result.Value ?? "", result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending DELETE request.");
            return HttpResult<bool>.FromException(ex);
        }
    }

    // POST multipart/form-data (для загрузки медиа)
    public async Task<HttpResult<string>> PostMultipartAsync(string endpoint, MultipartFormDataContent form)
    {
        try
        {
            return await SendWithAuthRetryAsync<string>(() => _httpClient.PostAsync(endpoint, form));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending multipart POST request.");
            return HttpResult<string>.FromException(ex);
        }
    }

    // GET (stream download, для скачивания медиа)
    public async Task<HttpResult<Utilities.FileResult>> GetFileStreamAsync(string endpoint)
    {
        try
        {
            async Task<HttpResponseMessage> SendAuthorizedAsync()
            {
                var token = _jwtProvider.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                return await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead);
            }

            var response = await SendAuthorizedAsync();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Access token expired. Trying to refresh JWT...");
                var refresh = await _jwtProvider.RefreshAsync();
                if (!refresh.IsSuccess)
                {
                    _logger.LogError("Token refresh failed.");
                    await _jwtProvider.ClearAsync();
                    return new HttpResult<Utilities.FileResult>(HttpStatusCode.Unauthorized);
                }

                _logger.LogInformation("JWT refreshed successfully. Retrying request...");
                response = await SendAuthorizedAsync();
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
