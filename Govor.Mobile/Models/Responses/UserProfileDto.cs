namespace Govor.Mobile.Models.Responses;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? IconId { get; set; }
    public bool IsOnline { get; set; }
}
