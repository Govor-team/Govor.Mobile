namespace Govor.Mobile.Models;

public class LocalUserProfile
{
    public required Guid UserId { get; set; }

    public Guid? AvatarId { get; set; }
    public string? AvatarPath { get; set; }

    public required string DisplayName { get; set; }
    public string? Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
