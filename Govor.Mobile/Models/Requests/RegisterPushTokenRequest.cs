namespace Govor.Mobile.Models.Requests;

public class RegisterPushTokenRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
}
