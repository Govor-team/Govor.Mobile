using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.Tests;

public class PrivateChatNavigationTests
{
    [Test]
    public async Task SameParticipantResolvesDifferentConversationsAfterAccountSwitch()
    {
        var session = new Session { CurrentUserId = Guid.NewGuid() };
        var api = new ChatApi();
        var chats = new PrivateChatApi(api, session);
        var friend = Guid.NewGuid();
        var first = await chats.GetChatByFriendId(friend);
        var cached = await chats.GetChatByFriendId(friend);
        session.CurrentUserId = Guid.NewGuid();
        var second = await chats.GetChatByFriendId(friend);
        Assert.Multiple(() =>
        {
            Assert.That(cached.Value, Is.EqualTo(first.Value));
            Assert.That(second.Value, Is.Not.EqualTo(first.Value));
            Assert.That(api.Reads, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ChangingAccountDuringRequestCannotOpenPreviousAccountsChat()
    {
        var session = new Session { CurrentUserId = Guid.NewGuid() };
        var api = new ChatApi { OnRead = () => session.CurrentUserId = Guid.NewGuid() };
        var result = await new PrivateChatApi(api, session).GetChatByFriendId(Guid.NewGuid());
        Assert.That(result.IsSuccess, Is.False);
    }

    private sealed class Session : IJwtProviderService
    {
        public Guid? CurrentUserId { get; set; }
        public bool HasRefreshToken => true;
        public event Action? WasClearTokens;
        public Task<string> GetAccessTokenAsync() => Task.FromResult("unused");
        public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => Task.FromResult("unused");
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
        public Task ClearAsync() { CurrentUserId = null; WasClearTokens?.Invoke(); return Task.CompletedTask; }
    }
    private sealed class ChatApi : IApiClient
    {
        public int Reads { get; private set; }
        public Action? OnRead { get; init; }
        public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true)
        { Reads++; OnRead?.Invoke(); return Task.FromResult(HttpResult<T>.Success((T)(object)Guid.NewGuid())); }
        public Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<Govor.Mobile.Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
    }
}
