using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Implementations.Profiles;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Tests
{
    public class DeviceSessionManagerTests
    {
        [Test]
        public async Task SameDeviceDescriptionNeverCausesAnotherSessionToBeCurrent()
        {
            var tokens = new Tokens();
            var other = Guid.NewGuid();
            var api = new Sessions { Items = [
                new(tokens.Id, "Phone on Android 16", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), false),
                new(other, "Phone on Android 16", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), false)] };
            var manager = new DeviceSessionManagerService(new DeviceInfoParserService(), new PlatformIconService(), tokens, api);
            var list = await manager.LoadSessionsAsync();
            Assert.That(list.Single(s => s.IsCurrent).Id, Is.EqualTo(tokens.Id));
            Assert.That(list.Single(s => s.Id == other).IsCurrent, Is.False);
            Assert.That(await manager.IsCurrentSessionAsync(other), Is.False);
            Assert.That(await manager.CloseSessionAsync(other), Is.True);
            Assert.That(api.Closed, Is.EqualTo(other));
            Assert.That(await manager.IsCurrentSessionAsync(tokens.Id), Is.True);
            tokens.Id = other;
            Assert.That(await manager.IsCurrentSessionAsync(other), Is.True, "Recheck the active JWT at action time.");
        }

        private sealed class Sessions : ISessionsService
        {
            public List<UserSession> Items = [];
            public Guid Closed;
            public Task<Result<List<UserSession>>> GetAllSessionsAsync() => Task.FromResult(Result<List<UserSession>>.Success(Items));
            public Task<Result<bool>> CloseSessionAsync(Guid id) { Closed = id; return Task.FromResult(Result<bool>.Success(true)); }
            public Task<Result<bool>> CloseCurrentSessionAsync() => throw new NotSupportedException();
            public Task<Result<bool>> CloseAllSessionsAsync() => throw new NotSupportedException();
        }

        private sealed class Tokens : IJwtProviderService
        {
            public Guid Id = Guid.NewGuid();
            public bool HasRefreshToken => true;
            public Guid? CurrentUserId => Guid.NewGuid();
            public event Action WasClearTokens { add { } remove { } }
            public Task<string> GetAccessTokenAsync() => Task.FromResult(new JwtSecurityTokenHandler().WriteToken(
                new JwtSecurityToken(claims: [new Claim("sid", Id.ToString())])));
            public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetAccessTokenAsync();
            public Task InitializeAsync() => Task.CompletedTask;
            public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
            public Task ClearAsync() => Task.CompletedTask;
        }
    }
}

namespace Govor.Mobile
{
    // Platform alert boundary only. The session manager under test is production code.
    public static class AppShell
    {
        public static Task DisplayException(string message) => throw new InvalidOperationException(message);
    }
}
