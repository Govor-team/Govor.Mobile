using System.Net;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Api.Base;

public class RefreshTokenHandler : DelegatingHandler
{
    private readonly IJwtProviderService _jwtProvider;
    private readonly SemaphoreSlim _refreshLock = new(1,1);

    public RefreshTokenHandler(IJwtProviderService jwtProvider)
    {
        _jwtProvider = jwtProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
           /* var refreshed = await _jwtProvider.TryRefreshTokenAsync();
            if (!refreshed)
                return response;

            var newRequest = await CloneRequestAsync(request);
            return await base.SendAsync(newRequest, cancellationToken);*/
           return response;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}