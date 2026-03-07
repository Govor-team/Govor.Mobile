namespace Govor.Mobile.Models.Requests;

public class RegisterPushTokenRequest
{
    public string Token { get; set; }
    public string Platform { get; set; }
}