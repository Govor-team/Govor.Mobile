namespace Govor.Mobile.Models.Results;

public record UserLogin(string username,
    string password,
    string refreshJwt);
