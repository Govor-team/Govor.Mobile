using System.Net.Http.Headers;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Api.Base;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IJwtProviderService _jwtProvider;

    public AuthHeaderHandler(IJwtProviderService jwtProvider)
    {
        _jwtProvider = jwtProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(
                HttpRequestOptionsKeys.RequireAuth,
                out bool requireAuth) && requireAuth)
        {
            var token = await _jwtProvider.GetAccessTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}