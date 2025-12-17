using System;
using System.Collections.Generic;
using System.Text;
using Govor.Mobile.Models.Responses;

namespace Govor.Mobile.Services.Api.Base
{
    public interface IApiClient
    {
        Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true);
        Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true);
        Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true);
        Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true);
        Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true);
        Task<HttpResult<Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true);
    }
}
