namespace Govor.Mobile.Application.Models.Results;

public record UserLogin(string username,
    string password,
    string refreshJwt);
