namespace Govor.Mobile.Models.Responses;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Description { get; set; }
    public DateTime WasOnline { get; set; }
    public Guid IconId {get; set;} 
    public bool IsOnline { get; set; }
}