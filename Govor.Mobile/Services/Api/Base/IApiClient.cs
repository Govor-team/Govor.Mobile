using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Api.Base
{
    public interface IApiClient
    {
        Task<HttpResult<T>> GetAsync<T>(string endpoint);
        Task<HttpResult<T>> PostAsync<T>(string endpoint, object data);
        Task<HttpResult<T>> PutAsync<T>(string endpoint, object data);
        Task<HttpResult<bool>> DeleteAsync(string endpoint);
    }
}
