namespace Govor.Mobile.Models.Results;

public class UserProfile
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid IconId { get; set; }
    public DateTime LastFetched { get; set; } 
}
