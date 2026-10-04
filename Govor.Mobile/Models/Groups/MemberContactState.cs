namespace Govor.Mobile.Models.Groups;

public enum MemberContactKind { Self, NewContact, IncomingRequest, OutgoingRequest, Friend }

public sealed record MemberContactState(MemberContactKind Kind, Guid? RequestId = null)
{
    public string ActionTitle => Kind switch
    {
        MemberContactKind.Friend => "Написать сообщение",
        MemberContactKind.IncomingRequest => "Принять заявку в друзья",
        MemberContactKind.OutgoingRequest => "Заявка отправлена",
        MemberContactKind.NewContact => "Добавить в друзья",
        _ => ""
    };
    public bool CanAct => Kind is MemberContactKind.Friend or MemberContactKind.IncomingRequest or MemberContactKind.NewContact;

    public static MemberContactState Resolve(bool isSelf, bool isFriend, Guid? incomingRequest, bool hasOutgoing) =>
        isSelf ? new(MemberContactKind.Self)
        : isFriend ? new(MemberContactKind.Friend)
        : incomingRequest.HasValue ? new(MemberContactKind.IncomingRequest, incomingRequest)
        : hasOutgoing ? new(MemberContactKind.OutgoingRequest)
        : new(MemberContactKind.NewContact);
}
