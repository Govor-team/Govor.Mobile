namespace Govor.Mobile.Models.Groups;

public enum GroupMemberStatus { Active, Banned, Left }

public sealed class GroupProfileChangedResponse
{
    public Guid GroupId { get; set; }
}

public sealed class GroupMemberChangedResponse
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupMemberStatus Status { get; set; }
    public GroupRole? Role { get; set; }
}
