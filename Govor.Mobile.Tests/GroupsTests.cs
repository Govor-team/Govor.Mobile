using System.Net;
using System.Text.Json;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.Tests;

public class GroupsTests
{
    [TestCase(null, false, false)]
    [TestCase(GroupRole.Member, false, true)]
    [TestCase(GroupRole.Admin, true, true)]
    [TestCase(GroupRole.Owner, true, true)]
    public void GroupMembershipGatesWritingAndModeration(GroupRole? role, bool moderator, bool writing)
    {
        var group = new GroupProfile { MyRole = role };
        Assert.That(GroupPermissions.CanModerate(group), Is.EqualTo(moderator));
        Assert.That(GroupPermissions.CanWrite(group), Is.EqualTo(writing));
    }
    [TestCase(GroupRole.Member, false)]
    [TestCase(GroupRole.Admin, true)]
    [TestCase(GroupRole.Owner, true)]
    public void ChannelMembersCannotPublish(GroupRole role, bool writing) =>
        Assert.That(GroupPermissions.CanWrite(new GroupProfile { IsChannel = true, MyRole = role }), Is.EqualTo(writing));

    [Test]
    public void AdminCannotBanAdminOwnerOrSelf()
    {
        var actor = Guid.NewGuid(); var other = Guid.NewGuid();
        var group = new GroupProfile { MyRole = GroupRole.Admin };
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = other, Role = GroupRole.Member }, actor), Is.True);
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = other, Role = GroupRole.Admin }, actor), Is.False);
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = other, Role = GroupRole.Owner }, actor), Is.False);
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = actor, Role = GroupRole.Member }, actor), Is.False);
        Assert.That(GroupPermissions.CanChangeRole(group, new() { UserId = other }, actor), Is.False);
    }
    [Test]
    public void OwnerCannotPromoteBannedMemberOrBanOwner()
    {
        var actor = Guid.NewGuid(); var other = Guid.NewGuid();
        var group = new GroupProfile { MyRole = GroupRole.Owner };
        Assert.That(GroupPermissions.CanChangeRole(group, new() { UserId = other, IsBanned = true }, actor), Is.False);
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = other, Role = GroupRole.Owner }, actor), Is.False);
        Assert.That(GroupPermissions.CanBan(group, new() { UserId = other, Role = GroupRole.Admin }, actor), Is.True);
    }
    [TestCase(" abc-123_ ")]
    [TestCase("http://localhost:7155/invite/abc-123_")]
    [TestCase("govor://invite/abc-123_")]
    public void InvitationAcceptsCodesAndTrustedLinks(string input) =>
        Assert.That(GroupInviteParser.Parse(input, "http://localhost:7155"), Is.EqualTo("abc-123_"));

    [TestCase("http://evil.example/invite/a")]
    [TestCase("http://localhost:7156/invite/a")]
    [TestCase("http://localhost:7155/api/auth/a")]
    [TestCase("http://localhost:7155/invite/a/b")]
    [TestCase("../a")]
    [TestCase("")]
    public void InvitationRejectsOtherHostsRoutesAndMalformedCodes(string input) =>
        Assert.Throws<ArgumentException>(() => GroupInviteParser.Parse(input, "http://localhost:7155"));

    [Test]
    public async Task InvitationPreviewIsReadOnlyAndJoinIsExplicitPost()
    {
        var api = new RecordingApi(); var groups = new GroupsApiService(api);
        await groups.PreviewInviteAsync("abc");
        Assert.That(api.Calls.Single(), Is.EqualTo(("GET", "/api/group-invites/abc")));
        await groups.JoinInviteAsync("abc");
        Assert.That(api.Calls.Last(), Is.EqualTo(("POST", "/api/group-invites/abc/join")));
    }
    [Test]
    public async Task PublicSearchEncodesQueryAndKeepsPagination()
    {
        var api = new RecordingApi(); var groups = new GroupsApiService(api);
        await groups.SearchAsync("новости & канал", 50);
        Assert.That(api.Calls.Single().Path, Is.EqualTo("/api/groups/search?q=" + Uri.EscapeDataString("новости & канал") + "&skip=50&take=50"));
    }
    [Test]
    public async Task ModerationAcceptsEmpty204AndUsesDocumentedRoutes()
    {
        var api = new RecordingApi(); var groups = new GroupsApiService(api);
        var group = Guid.NewGuid(); var user = Guid.NewGuid();
        await groups.SetAdminAsync(group, user, true);
        await groups.SetAdminAsync(group, user, false);
        await groups.SetBanAsync(group, user, true);
        await groups.TransferAsync(group, user);
        Assert.That(api.Calls, Is.EqualTo(new[] {
            ("PUT", $"/api/groups/{group}/members/{user}/administrator"),
            ("DELETE", $"/api/groups/{group}/members/{user}/administrator"),
            ("PUT", $"/api/groups/{group}/members/{user}/ban"),
            ("POST", $"/api/groups/{group}/ownership/{user}") }));
    }
    [Test]
    public async Task ProfileUpdateCannotChangeCommunityType()
    {
        var api = new RecordingApi();
        await new GroupsApiService(api).SaveAsync(Guid.NewGuid(), " News ", "desc", false);
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(api.Body));
        Assert.That(body.RootElement.GetProperty("name").GetString(), Is.EqualTo("News"));
        Assert.That(body.RootElement.TryGetProperty("isChannel", out _), Is.False);
    }
    [Test]
    public void DeniedRequestDoesNotReportSuccess()
    {
        var api = new RecordingApi { Failure = HttpStatusCode.Forbidden };
        var error = Assert.ThrowsAsync<GroupApiException>(() => new GroupsApiService(api).GetAsync(Guid.NewGuid()));
        Assert.That(error!.Status, Is.EqualTo(HttpStatusCode.Forbidden));
    }
    [TestCase(0)]
    [TestCase(1)]
    public async Task SwitchingAwayFromSelectedReactionsClearsIdsInRequest(int mode)
    {
        var api = new RecordingApi();
        await new GroupsApiService(api).SetPolicyAsync(Guid.NewGuid(), mode, new[] { Guid.NewGuid() });
        using var body = JsonDocument.Parse(JsonSerializer.Serialize(api.Body));
        Assert.That(body.RootElement.GetProperty("mode").GetInt32(), Is.EqualTo(mode));
        Assert.That(body.RootElement.GetProperty("reactionIds").GetArrayLength(), Is.Zero);
    }
    [Test]
    public void SelectingMoreThanServerLimitDoesNotSendInvalidPolicy()
    {
        var api = new RecordingApi();
        Assert.ThrowsAsync<ArgumentException>(() => new GroupsApiService(api).SetPolicyAsync(
            Guid.NewGuid(), 2, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid())));
        Assert.That(api.Calls, Is.Empty);
    }
    [Test]
    public void ExpiredRevokedAndFullInvitationsCannotBeShared()
    {
        Assert.That(new GroupInvitation { EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 0, UsedCount = 15 }.IsActive, Is.True);
        Assert.That(new GroupInvitation { EndDate = DateTime.UtcNow.AddDays(-1) }.IsActive, Is.False);
        Assert.That(new GroupInvitation { EndDate = DateTime.UtcNow.AddDays(1), IsRevoked = true }.IsActive, Is.False);
        Assert.That(new GroupInvitation { EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 2, UsedCount = 2 }.IsActive, Is.False);
    }

    private sealed class RecordingApi : IApiClient
    {
        public List<(string Method, string Path)> Calls { get; } = new();
        public object? Body { get; private set; }
        public HttpStatusCode? Failure { get; init; }
        private Task<HttpResult<T>> Record<T>(string method, string path, object? body = null)
        {
            Calls.Add((method, path)); Body = body;
            if (Failure is HttpStatusCode status) return Task.FromResult(new HttpResult<T>("Denied", status));
            object? value = typeof(T) == typeof(GroupProfile) ? new GroupProfile()
                : typeof(T) == typeof(List<GroupProfile>) ? new List<GroupProfile>() : default;
            return Task.FromResult(HttpResult<T>.Success((T)value!));
        }
        public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true) => Record<T>("GET", endpoint);
        public Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true) => Record<T>("POST", endpoint, data);
        public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true) => Record<T>("PUT", endpoint, data);
        public Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true)
        { Calls.Add(("DELETE", endpoint)); return Task.FromResult(HttpResult<bool>.Success(true)); }
        public Task<HttpResult<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent form, bool authenticated = true) => Record<T>("POST", endpoint);
        public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true) => Record<UploadMediaResponse>("POST", endpoint);
        public Task<HttpResult<Govor.Mobile.Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
    }
}
