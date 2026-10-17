using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Implementations.Notification;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Tests;

public class PushNotificationTests
{
    [Test]
    public void DuplicateDeliveryAndTapHaveIndependentPersistentHistories()
    {
        var id = Guid.NewGuid();
        Assert.That(PushMessageHistory.TryRemember("[]", id, out var received), Is.True);
        Assert.That(PushMessageHistory.TryRemember(received, id, out _), Is.False);
        Assert.That(PushMessageHistory.TryRemember("[]", id, out var opened), Is.True);
        Assert.That(PushMessageHistory.TryRemember(opened, id, out _), Is.False);
    }

    [Test]
    public void HistoryRecoversFromCorruptionAndHasBoundedSize()
    {
        var oldest = Guid.NewGuid();
        Assert.That(PushMessageHistory.TryRemember("corrupted", oldest, out var history), Is.True);
        for (var i = 0; i < PushMessageHistory.Limit; i++)
            PushMessageHistory.TryRemember(history, Guid.NewGuid(), out history);
        Assert.That(System.Text.Json.JsonSerializer.Deserialize<Guid[]>(history), Has.Length.EqualTo(PushMessageHistory.Limit));
        Assert.That(PushMessageHistory.TryRemember(history, oldest, out _), Is.True);
    }
    [Test]
    public async Task RegisterAndDeleteAcceptNoContentAndUseAuthenticatedSessionEndpoints()
    {
        var api = new Api();
        var pushes = new PushTokenService(api);
        Assert.That((await pushes.PushToken("fcm", "Android")).IsSuccess, Is.True);
        Assert.That(api.Endpoint, Is.EqualTo("/api/pushes/token/register"));
        Assert.That(api.Authenticated, Is.True);
        Assert.That(api.Body, Is.TypeOf<RegisterPushTokenRequest>());
        Assert.That(((RegisterPushTokenRequest)api.Body!).Platform, Is.EqualTo("Android"));
        Assert.That(System.Text.Json.JsonSerializer.Serialize(api.Body),
            Is.EqualTo("{\"token\":\"fcm\",\"platform\":\"Android\"}"));
        Assert.That((await pushes.DeleteTokenAsync()).IsSuccess, Is.True);
        Assert.That(api.Endpoint, Is.EqualTo("/api/pushes/token"));
        Assert.That(api.Authenticated, Is.True);
        Assert.That((await pushes.DeleteTokenAsync()).IsSuccess, Is.True);
    }

    [TestCase("", "Android")]
    [TestCase("   ", "Android")]
    [TestCase("fcm", " ")]
    public async Task InvalidRegistrationDoesNotCallApi(string token, string platform)
    {
        var api = new Api();
        Assert.That((await new PushTokenService(api).PushToken(token, platform)).IsSuccess, Is.False);
        Assert.That(api.Calls, Is.Zero);
    }

    [Test]
    public async Task EnforcesLengthLimits()
    {
        var api = new Api();
        var pushes = new PushTokenService(api);
        Assert.That((await pushes.PushToken(new string('x', 513), "Android")).IsSuccess, Is.False);
        Assert.That((await pushes.PushToken("fcm", new string('x', 51))).IsSuccess, Is.False);
        Assert.That(api.Calls, Is.Zero);
        Assert.That((await pushes.PushToken(new string('x', 512), new string('x', 50))).IsSuccess, Is.True);
    }

    [Test]
    public async Task SameTokenIsRegisteredAgainForNewSessionAndNewAccount()
    {
        var session = new Session();
        var api = new Api();
        var registration = new PushTokenRegistration(session, new PushTokenService(api));
        await registration.RegisterAsync("fcm", "Android");
        await registration.RegisterAsync("fcm", "Android");
        Assert.That(api.Calls, Is.EqualTo(1));
        session.Sid = Guid.NewGuid().ToString();
        await registration.RegisterAsync("fcm", "Android");
        Assert.That(api.Calls, Is.EqualTo(2));
        session.CurrentUserId = Guid.NewGuid();
        await registration.RegisterAsync("fcm", "Android");
        Assert.That(api.Calls, Is.EqualTo(3));
        await registration.RegisterAsync("rotated-fcm", "Android");
        Assert.That(api.Calls, Is.EqualTo(4));
    }

    [Test]
    public async Task FailedRegistrationIsRetriedAndConcurrentCallbacksAreSerialized()
    {
        var api = new Api { Fails = true };
        var registration = new PushTokenRegistration(new Session(), new PushTokenService(api));
        Assert.That(await registration.RegisterAsync("fcm", "iOS"), Is.False);
        api.Fails = false;
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => registration.RegisterAsync("fcm", "iOS")));
        Assert.That(api.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task DisableInvalidatesCacheAndUnauthenticatedCallbacksNeverRegister()
    {
        var session = new Session();
        var api = new Api();
        var registration = new PushTokenRegistration(session, new PushTokenService(api));
        await registration.RegisterAsync("fcm", "Android");
        Assert.That(await registration.UnregisterAsync(), Is.True);
        await registration.RegisterAsync("fcm", "Android");
        Assert.That(api.Calls, Is.EqualTo(3));
        session.HasRefreshToken = false;
        Assert.That(await registration.RegisterAsync("other", "Android"), Is.False);
        await registration.UnregisterAsync();
        Assert.That(api.Calls, Is.EqualTo(3));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ParsesDocumentedChatPayload(bool group)
    {
        var data = Payload(group);
        Assert.That(ChatPushNotification.TryParse(data, out var push), Is.True);
        Assert.That(push!.IsGroup, Is.EqualTo(group));
        Assert.That(push.MessageId.ToString(), Is.EqualTo(data["messageId"]));
    }

    [TestCase("type", "reaction")]
    [TestCase("chatId", "invalid")]
    [TestCase("messageId", "00000000-0000-0000-0000-000000000000")]
    [TestCase("senderId", "")]
    [TestCase("isGroup", "yes")]
    public void RejectsMalformedOrUnsupportedPayload(string key, string value)
    {
        var data = Payload(false);
        data[key] = value;
        Assert.That(ChatPushNotification.TryParse(data, out _), Is.False);
        data.Remove(key);
        Assert.That(ChatPushNotification.TryParse(data, out _), Is.False);
    }

    private static Dictionary<string, object> Payload(bool group) => new()
    {
        ["type"] = "chat_message", ["chatId"] = Guid.NewGuid().ToString(),
        ["messageId"] = Guid.NewGuid().ToString(), ["senderId"] = Guid.NewGuid().ToString(),
        ["isGroup"] = group.ToString().ToLowerInvariant()
    };

    private sealed class Session : IJwtProviderService
    {
        public Guid? CurrentUserId { get; set; } = Guid.NewGuid();
        public string Sid { get; set; } = Guid.NewGuid().ToString();
        public bool HasRefreshToken { get; set; } = true;
        public event Action? WasClearTokens;
        public Task<string> GetAccessTokenAsync() => Task.FromResult(new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(claims: [new Claim("sid", Sid), new Claim("userId", CurrentUserId.ToString()!)])));
        public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetAccessTokenAsync();
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
        public Task ClearAsync() { HasRefreshToken = false; WasClearTokens?.Invoke(); return Task.CompletedTask; }
    }

    private sealed class Api : IApiClient
    {
        public int Calls;
        public bool Fails;
        public string? Endpoint;
        public bool Authenticated;
        public object? Body;
        public async Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true)
        {
            Calls++; Endpoint = endpoint; Body = data; Authenticated = authenticated;
            await Task.Yield();
            return Fails ? new HttpResult<T>("offline", System.Net.HttpStatusCode.ServiceUnavailable)
                : HttpResult<T>.Success(default!); // 204 has no bool body.
        }
        public Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true)
        { Calls++; Endpoint = endpoint; Authenticated = authenticated; return Task.FromResult(HttpResult<bool>.Success(true)); }
        public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<Govor.Mobile.Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
    }
}
