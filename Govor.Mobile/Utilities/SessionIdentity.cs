using System.IdentityModel.Tokens.Jwt;

namespace Govor.Mobile.Utilities;

public static class SessionIdentity
{
    public static Guid ReadSessionId(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var claims = token.Claims.Where(c => c.Type == "sid").ToArray();
        if (claims.Length != 1 || !Guid.TryParse(claims[0].Value, out var id) || id == Guid.Empty)
            throw new InvalidDataException("Не удалось определить текущую сессию.");
        return id;
    }
}
