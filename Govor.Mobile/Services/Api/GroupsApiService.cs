using System.Net;
using System.Text.Json;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.Services.Api;

public sealed class GroupApiException(string message, HttpStatusCode status) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed class GroupsApiService(IApiClient api)
{
    private static string Root(Guid id) => $"/api/groups/{id}";
    private static T Require<T>(HttpResult<T> result)
    {
        Check(result);
        return result.Value ?? throw new InvalidOperationException("Сервер вернул пустой ответ.");
    }
    private static void Check<T>(HttpResult<T> result)
    {
        if (result.IsSuccess) return;
        var message = result.ErrorMessage ?? "Не удалось выполнить запрос.";
        try
        {
            using var json = JsonDocument.Parse(message);
            if (json.RootElement.TryGetProperty("detail", out var detail)) message = detail.GetString() ?? message;
            else if (json.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
        }
        catch (JsonException) { }
        throw new GroupApiException(message, result.StatusCode);
    }
    public async Task<List<GroupProfile>> MineAsync(int skip = 0) =>
        Require(await api.GetAsync<List<GroupProfile>>($"/api/groups/mine?skip={skip}&take=50"));
    public async Task<List<GroupProfile>> SearchAsync(string query, int skip = 0) =>
        Require(await api.GetAsync<List<GroupProfile>>($"/api/groups/search?q={Uri.EscapeDataString(query.Trim())}&skip={skip}&take=50"));
    public async Task<GroupProfile> GetAsync(Guid id) => Require(await api.GetAsync<GroupProfile>(Root(id)));
    public async Task<GroupProfile> CreateAsync(string name, string description, bool isPrivate, bool isChannel) =>
        Require(await api.PostAsync<GroupProfile>("/api/groups", new { name = name.Trim(), description, isPrivate, isChannel }));
    public async Task<GroupProfile> SaveAsync(Guid id, string name, string description, bool isPrivate) =>
        Require(await api.PutAsync<GroupProfile>(Root(id), new { name = name.Trim(), description, isPrivate }));
    public async Task<GroupProfile> JoinAsync(Guid id) => Require(await api.PostAsync<GroupProfile>(Root(id) + "/join", new { }));
    public async Task LeaveAsync(Guid id) => Check(await api.DeleteAsync(Root(id) + "/members/me"));
    public async Task<List<GroupMember>> MembersAsync(Guid id, bool bannedOnly, int skip = 0) =>
        Require(await api.GetAsync<List<GroupMember>>($"{Root(id)}/members?bannedOnly={bannedOnly.ToString().ToLowerInvariant()}&skip={skip}&take=50"));
    public async Task SetAdminAsync(Guid id, Guid user, bool enabled)
    {
        var path = $"{Root(id)}/members/{user}/administrator";
        if (enabled) Check(await api.PutAsync<object>(path, new { }));
        else Check(await api.DeleteAsync(path));
    }
    public async Task SetBanAsync(Guid id, Guid user, bool enabled)
    {
        var path = $"{Root(id)}/members/{user}/ban";
        if (enabled) Check(await api.PutAsync<object>(path, new { }));
        else Check(await api.DeleteAsync(path));
    }
    public async Task TransferAsync(Guid id, Guid user) => Check(await api.PostAsync<object>($"{Root(id)}/ownership/{user}", new { }));
    public async Task RemoveMessageAsync(Guid id, Guid message) => Check(await api.DeleteAsync($"{Root(id)}/messages/{message}"));
    public async Task<GroupProfile> PreviewInviteAsync(string code) => Require(await api.GetAsync<GroupProfile>($"/api/group-invites/{Uri.EscapeDataString(code)}"));
    public async Task<GroupProfile> JoinInviteAsync(string code) => Require(await api.PostAsync<GroupProfile>($"/api/group-invites/{Uri.EscapeDataString(code)}/join", new { }));
    public async Task<List<GroupInvitation>> InvitationsAsync(Guid id) => Require(await api.GetAsync<List<GroupInvitation>>(Root(id) + "/invitations"));
    public async Task<GroupInvitation> CreateInvitationAsync(Guid id, int days, int limit, string description) =>
        Require(await api.PostAsync<GroupInvitation>(Root(id) + "/invitations", new { validForDays = days, maxParticipants = limit, description }));
    public async Task RevokeInvitationAsync(Guid id, Guid invitation) => Check(await api.DeleteAsync($"{Root(id)}/invitations/{invitation}"));
    public async Task<GroupAvatarResponse> UploadAvatarAsync(Guid id, MultipartFormDataContent form) =>
        Require(await api.PostMultipartAsync<GroupAvatarResponse>(Root(id) + "/avatar", form));
    public async Task DeleteAvatarAsync(Guid id) => Check(await api.DeleteAsync(Root(id) + "/avatar"));
    public async Task<ChannelReactionPolicy> PolicyAsync(Guid id) => Require(await api.GetAsync<ChannelReactionPolicy>(Root(id) + "/reactions"));
    public async Task SetPolicyAsync(Guid id, int mode, IEnumerable<Guid> reactionIds)
    {
        // All/None must not send the remembered selection to the server.
        var selected = mode == 2 ? reactionIds.Distinct().ToArray() : Array.Empty<Guid>();
        if (selected.Length > 100)
            throw new ArgumentException("Для канала можно выбрать не больше 100 реакций.");
        Check(await api.PutAsync<object>(Root(id) + "/reactions", new { mode, reactionIds = selected }));
    }
}
