namespace Govor.Mobile.Models.Groups;

public enum GroupRole { Member, Admin, Owner }

public sealed class GroupProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? ImageId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsChannel { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public GroupRole? MyRole { get; set; }
    public bool IsRequiredChannel { get; set; }
    public bool CanLeave { get; set; }
    public string Summary => $"{(IsChannel ? "Канал" : "Группа")} · {(IsPrivate ? "Приватная" : "Публичная")} · {MemberCount} участников";
}

public sealed class GroupMember
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public GroupRole Role { get; set; }
    public bool IsBanned { get; set; }
    public DateTime MemberSince { get; set; }
    public string RoleLabel => IsBanned ? "Заблокирован" : Role switch
    {
        GroupRole.Owner => "Владелец", GroupRole.Admin => "Администратор", _ => "Участник"
    };
}

public sealed class GroupInvitation
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InvitationCode { get; set; } = "";
    public string SharePath { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EndDate { get; set; }
    public int MaxParticipants { get; set; }
    public int UsedCount { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive => !IsRevoked && EndDate > DateTime.UtcNow && (MaxParticipants == 0 || UsedCount < MaxParticipants);
    public string Summary => $"{(IsRevoked ? "Отозвана" : EndDate <= DateTime.UtcNow ? "Истекла" : "До " + EndDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm"))} · {UsedCount}/{(MaxParticipants == 0 ? "∞" : MaxParticipants)} вступлений";
}

public sealed class GroupAvatarResponse
{
    public Guid ImageId { get; set; }
    public string MediaUrl { get; set; } = "";
}

public static class GroupPermissions
{
    public static bool IsOwner(GroupProfile? group) => group?.MyRole == GroupRole.Owner;
    public static bool CanModerate(GroupProfile? group) => group?.MyRole is GroupRole.Admin or GroupRole.Owner;
    public static bool CanWrite(GroupProfile? group) => group?.MyRole != null && (!group.IsChannel || CanModerate(group));
    public static bool CanBan(GroupProfile? group, GroupMember member, Guid actor) =>
        member.UserId != actor && member.Role != GroupRole.Owner && CanModerate(group)
        && (IsOwner(group) || member.Role == GroupRole.Member);
    public static bool CanChangeRole(GroupProfile? group, GroupMember member, Guid actor) =>
        IsOwner(group) && member.UserId != actor && member.Role != GroupRole.Owner && !member.IsBanned;
}
