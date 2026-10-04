using Govor.Mobile.Models.Groups;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Implementations;

public sealed class GroupMemberContactService(IFriendshipApiService friends, IFriendsRequestQueryService requests,
    IFriendsHubService hub, IPrivateChatApi chats, IJwtProviderService session)
{
    public async Task<MemberContactState> GetStateAsync(Guid userId)
    {
        if (session.CurrentUserId == userId) return new(MemberContactKind.Self);
        var list = await friends.GetFriends();
        if (!list.IsSuccess) throw new InvalidOperationException(list.ErrorMessage ?? "Не удалось проверить список друзей.");
        if (list.Value.Any(u => u.Id == userId)) return new(MemberContactKind.Friend);
        var incomingTask = requests.GetIncomingRequests();
        var outgoingTask = requests.GetResponses();
        await Task.WhenAll(incomingTask, outgoingTask);
        var incoming = await incomingTask; var outgoing = await outgoingTask;
        if (!incoming.IsSuccess || !outgoing.IsSuccess)
            throw new InvalidOperationException("Не удалось проверить заявки в друзья. Попробуйте ещё раз.");
        return MemberContactState.Resolve(false, false,
            incoming.Value.FirstOrDefault(r => r.RequesterId == userId && r.Status == FriendshipStatus.Pending)?.Id,
            outgoing.Value.Any(r => r.AddresseeId == userId && r.Status == FriendshipStatus.Pending));
    }

    public async Task<string> ActAsync(Guid userId, MemberContactKind intended)
    {
        // The relationship may have changed on another device while the menu was open.
        var state = await GetStateAsync(userId);
        // Revalidation must never turn "Write" into "Send request" or "Accept" into a new invitation.
        if (state.Kind != intended)
            return state.Kind switch
            {
                MemberContactKind.Friend => "Пользователь уже в друзьях. Откройте профиль ещё раз.",
                MemberContactKind.OutgoingRequest => "Заявка уже отправлена.",
                MemberContactKind.IncomingRequest => "Есть входящая заявка. Откройте профиль ещё раз.",
                _ => "Статус дружбы изменился. Откройте профиль ещё раз."
            };
        if (state.Kind == MemberContactKind.Friend)
        {
            var chat = await chats.GetChatByFriendId(userId);
            if (!chat.IsSuccess || chat.Value == Guid.Empty)
                throw new InvalidOperationException(chat.ErrorMessage ?? "Не удалось открыть переписку.");
            await Shell.Current.GoToAsync($"memberChat?chatId={chat.Value}&peerId={userId}&isGroup=false", false);
            return "";
        }
        if (!state.CanAct) return state.ActionTitle;
        var result = state.Kind == MemberContactKind.IncomingRequest
            ? await hub.AcceptRequestAsync(state.RequestId!.Value)
            : await hub.SendRequestAsync(userId);
        if (result.Status is HubResultStatus.Success or HubResultStatus.Created or HubResultStatus.NoContent)
            return state.Kind == MemberContactKind.IncomingRequest ? "Заявка принята. Пользователь добавлен в друзья." : "Заявка в друзья отправлена.";
        if (result.Status == HubResultStatus.Conflict)
        {
            var current = await GetStateAsync(userId);
            if (current.Kind == MemberContactKind.Friend) return "Пользователь уже в друзьях.";
            if (current.Kind == MemberContactKind.OutgoingRequest) return "Заявка уже отправлена.";
        }
        throw new InvalidOperationException(result.ErrorMessage ?? "Не удалось выполнить действие с заявкой.");
    }
}
