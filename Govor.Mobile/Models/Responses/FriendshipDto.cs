namespace Govor.Mobile.Models.Responses;

public class FriendshipDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; }
}

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected,
    Blocked
}
