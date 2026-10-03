using System.Net;
using System.Net.Http.Headers;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Api.Base;

public sealed class RefreshTokenHandler(IJwtProviderService jwtProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authenticated = request.Options.TryGetValue(HttpRequestOptionsKeys.RequireAuth, out var required) && required;
        // Buffer once before sending so multipart and stream bodies can be replayed exactly once.
        if (authenticated && request.Content is not null)
            await request.Content.LoadIntoBufferAsync();
        var response = await base.SendAsync(request, cancellationToken);
        if (!authenticated || response.StatusCode != HttpStatusCode.Unauthorized)
            return response;
        string token;
        try
        {
            token = await jwtProvider.RefreshAccessTokenAsync(request.Headers.Authorization?.Parameter);
        }
        catch
        {
            response.Dispose();
            throw;
        }
        response.Dispose();
        using var retry = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };
        foreach (var option in request.Options)
            retry.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        foreach (var header in request.Headers)
            retry.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (request.Content is not null)
        {
            retry.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            foreach (var header in request.Content.Headers)
                retry.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(retry, cancellationToken);
    }
}
