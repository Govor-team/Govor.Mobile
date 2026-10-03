using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Implementations.JwtServices;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Govor.Mobile.Tests;

public class SessionTests
{
    private static readonly Guid UserId = Guid.Parse("c62c10f0-1e76-4e06-9388-8be4a53df689");
    private static string Token(int minutes = 60) => new JwtSecurityTokenHandler().WriteToken(
        new JwtSecurityToken(claims: [new Claim("userId", UserId.ToString()), new Claim("jti", Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(minutes)));

    private sealed class Storage : ITokenStorageService
    {
        public string? Access;
        public string? Refresh;
        public bool SaveSucceeds = true;
        public Task<string?> GetAccessTokenAsync() => Task.FromResult(Access);
        public Task<string?> GetRefreshTokenAsync() => Task.FromResult(Refresh);
        public Task<bool> SaveTokensAsync(string accessToken, string refreshToken)
        {
            if (SaveSucceeds) { Access = accessToken; Refresh = refreshToken; }
            return Task.FromResult(SaveSucceeds);
        }
        public Task<bool> SaveRefreshTokenAsync(string token) { Refresh = token; return Task.FromResult(true); }
        public bool DeleteRefreshToken() { Access = null; Refresh = null; return true; }
    }
    private sealed class Server : IServerIpProvider { public string IP => "https://govor.test"; }
    private sealed class Factory(HttpClient client) : IHttpClientFactory { public HttpClient CreateClient(string name) => client; }
    private sealed class Transport(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Interlocked.Increment(ref Calls); return send(request); }
    }
    private static JwtProviderService Provider(Storage storage, Transport transport) => new(
        NullLogger<JwtProviderService>.Instance, new Server(), storage, new Factory(new HttpClient(transport)));
    private static HttpResponseMessage Tokens(string access) => new(HttpStatusCode.OK)
        { Content = JsonContent.Create(new { accessToken = access, refreshToken = "rotated" }) };

    [Test]
    public async Task PersistedAccessTokenRestoresWithoutNetwork()
    {
        var storage = new Storage { Access = Token(), Refresh = "saved" };
        var transport = new Transport(_ => throw new HttpRequestException("Offline"));
        var provider = Provider(storage, transport);
        await provider.InitializeAsync();
        Assert.That(await provider.GetAccessTokenAsync(), Is.EqualTo(storage.Access));
        Assert.That(provider.CurrentUserId, Is.EqualTo(UserId));
        Assert.That(transport.Calls, Is.Zero);
    }

    [Test]
    public async Task ConcurrentExpiredRequestsRotateOnce()
    {
        var expected = Token();
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var transport = new Transport(async _ => { await Task.Delay(20); return Tokens(expected); });
        var provider = Provider(storage, transport);
        await provider.InitializeAsync();
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => provider.GetAccessTokenAsync()));
        Assert.That(results, Is.All.EqualTo(expected));
        Assert.That(transport.Calls, Is.EqualTo(1));
        Assert.That(storage.Refresh, Is.EqualTo("rotated"));
    }

    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    public async Task TransientRefreshFailureKeepsSession(HttpStatusCode status)
    {
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var transport = new Transport(_ => Task.FromResult(new HttpResponseMessage(status)));
        var provider = Provider(storage, transport);
        await provider.InitializeAsync();
        Assert.ThrowsAsync<HttpRequestException>(() => provider.GetAccessTokenAsync());
        Assert.That(provider.HasRefreshToken, Is.True);
        Assert.That(storage.Refresh, Is.EqualTo("saved"));
    }

    [Test]
    public async Task NetworkFailureKeepsSession()
    {
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var provider = Provider(storage, new Transport(_ => throw new HttpRequestException("Offline")));
        await provider.InitializeAsync();
        Assert.ThrowsAsync<HttpRequestException>(() => provider.GetAccessTokenAsync());
        Assert.That(storage.Refresh, Is.EqualTo("saved"));
    }

    [Test]
    public async Task InvalidRefreshProblemClearsSessionAndNotifiesOnce()
    {
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var provider = Provider(storage, new Transport(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = JsonContent.Create(new { errorCode = "Auth.InvalidToken" }) })));
        var notifications = 0;
        provider.WasClearTokens += () => notifications++;
        await provider.InitializeAsync();
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => provider.GetAccessTokenAsync());
        Assert.That(provider.HasRefreshToken, Is.False);
        Assert.That(storage.Refresh, Is.Null);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public async Task UnknownBadRequestDoesNotEraseSession()
    {
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var provider = Provider(storage, new Transport(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            { Content = JsonContent.Create(new { errorCode = "Unexpected.Error" }) })));
        await provider.InitializeAsync();
        Assert.ThrowsAsync<HttpRequestException>(() => provider.GetAccessTokenAsync());
        Assert.That(storage.Refresh, Is.EqualTo("saved"));
    }

    [Test]
    public void PersistenceFailureDoesNotPublishNewTokens()
    {
        var storage = new Storage { SaveSucceeds = false };
        var provider = Provider(storage, new Transport(_ => Task.FromResult(Tokens(Token()))));
        Assert.ThrowsAsync<IOException>(() => provider.InitializeWithTokensAsync(Token(), "refresh"));
        Assert.That(provider.HasRefreshToken, Is.False);
    }

    [Test]
    public async Task LogoutWaitsForRotationAndCannotBeUndoneByItsResponse()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var storage = new Storage { Access = Token(-1), Refresh = "saved" };
        var provider = Provider(storage, new Transport(async _ => { started.SetResult(); await release.Task; return Tokens(Token()); }));
        await provider.InitializeAsync();
        var refresh = provider.GetAccessTokenAsync();
        await started.Task;
        var logout = provider.ClearAsync();
        release.SetResult();
        await Task.WhenAll(refresh, logout);
        Assert.That(provider.HasRefreshToken, Is.False);
        Assert.That(storage.Access, Is.Null);
    }

    [Test]
    public async Task UnauthorizedRequestRetriesOnceWithIdenticalBodyAndContentHeaders()
    {
        var oldToken = Token();
        var newToken = Token();
        var storage = new Storage { Access = oldToken, Refresh = "saved" };
        var provider = Provider(storage, new Transport(_ => Task.FromResult(Tokens(newToken))));
        await provider.InitializeAsync();
        var bodies = new List<string>();
        var headers = new List<string?>();
        var tokens = new List<string?>();
        var api = new Transport(async request =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync());
            headers.Add(request.Content.Headers.ContentType?.ToString());
            tokens.Add(request.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(tokens.Count == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
        });
        using var client = new HttpClient(new AuthHeaderHandler(provider)
            { InnerHandler = new RefreshTokenHandler(provider) { InnerHandler = api } });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://govor.test/message")
            { Content = JsonContent.Create(new { text = "Привет" }) };
        request.Options.Set(HttpRequestOptionsKeys.RequireAuth, true);
        using var response = await client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(tokens, Is.EqualTo(new[] { oldToken, newToken }));
        Assert.That(bodies[0], Is.EqualTo(bodies[1]));
        Assert.That(headers[0], Is.EqualTo(headers[1]));
    }

    [Test]
    public async Task AnonymousUnauthorizedRequestDoesNotRefresh()
    {
        var storage = new Storage();
        var refresh = new Transport(_ => Task.FromResult(Tokens(Token())));
        var provider = Provider(storage, refresh);
        var api = new Transport(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var client = new HttpClient(new RefreshTokenHandler(provider) { InnerHandler = api });
        using var response = await client.GetAsync("https://govor.test/login");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(refresh.Calls, Is.Zero);
        Assert.That(api.Calls, Is.EqualTo(1));
    }
}
