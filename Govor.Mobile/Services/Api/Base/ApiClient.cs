using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Models.Responses;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Govor.Mobile.Services.Api.Base;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(
        HttpClient httpClient,
        IServerIpProvider ipProvider,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // BaseAddress через DI
        _httpClient.BaseAddress = new Uri(ipProvider.IP);
    }

    // -----------------------------
    // CORE SEND
    // -----------------------------
    private async Task<HttpResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Request failed → {StatusCode} | {Error}",
                    response.StatusCode,
                    error);
                return new HttpResult<T>(error, response.StatusCode);
            }

            if (response.Content == null)
                return HttpResult<T>.Success(default);

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return HttpResult<T>.Success(default);

            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return HttpResult<T>.Success(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed");
            return HttpResult<T>.FromException(ex);
        }
    }

    // -----------------------------
    // REQUEST FACTORY
    // -----------------------------
    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string endpoint,
        bool authenticated = true,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, endpoint);

        if (content != null)
            request.Content = content;

        // Этот флаг обрабатывается AuthHeaderHandler
        request.Options.Set(HttpRequestOptionsKeys.RequireAuth, authenticated);

        return request;
    }

    // -----------------------------
    // GET
    // -----------------------------
    public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true)
    {
        var request = CreateRequest(HttpMethod.Get, endpoint, authenticated);
        return SendAsync<T>(request);
    }

    // -----------------------------
    // POST
    // -----------------------------
    public Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true)
    {
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var request = CreateRequest(HttpMethod.Post, endpoint, authenticated, content);
        return SendAsync<T>(request);
    }

    // -----------------------------
    // PUT
    // -----------------------------
    public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true)
    {
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        var request = CreateRequest(HttpMethod.Put, endpoint, authenticated, content);
        return SendAsync<T>(request);
    }

    // -----------------------------
    // DELETE
    // -----------------------------
    public async Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true)
    {
        var request = CreateRequest(HttpMethod.Delete, endpoint, authenticated);
        var result = await SendAsync<string>(request);

        return result.IsSuccess
            ? HttpResult<bool>.Success(true)
            : new HttpResult<bool>(false, result.Value ?? "", result.StatusCode);
    }

    // -----------------------------
    // MULTIPART (UPLOAD)
    // -----------------------------
    public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(
        string endpoint,
        MultipartFormDataContent form,
        bool authenticated = true)
    {
        var request = CreateRequest(HttpMethod.Post, endpoint, authenticated, form);
        return SendAsync<UploadMediaResponse>(request);
    }

    // -----------------------------
    // STREAM DOWNLOAD
    // -----------------------------
    public async Task<HttpResult<Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, endpoint, authenticated);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new HttpResult<Utilities.FileResult>(error, response.StatusCode);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var contentDisposition = response.Content.Headers.ContentDisposition;

            var fileName =
                contentDisposition?.FileNameStar ??
                contentDisposition?.FileName ??
                "downloaded_file";

            var mimeType = response.Content.Headers.ContentType?.MediaType;

            return HttpResult<Utilities.FileResult>.Success(
                new Utilities.FileResult(stream, fileName, mimeType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File download failed");
            return HttpResult<Utilities.FileResult>.FromException(ex);
        }
    }
}