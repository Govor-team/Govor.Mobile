using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Govor.Mobile.Tests;

public class SessionIdentityTests
{
    [Test]
    public void SessionsOnTheSameDeviceAreDistinguishedBySid()
    {
        var current = Guid.NewGuid();
        var other = Guid.NewGuid();
        var token = Token(new Claim("sid", current.ToString()), new Claim("deviceInfo", "Same phone on Android 16"));
        Assert.That(SessionIdentity.ReadSessionId(token), Is.EqualTo(current).And.Not.EqualTo(other));
    }

    [TestCase("")]
    [TestCase("not-a-guid")]
    [TestCase("00000000-0000-0000-0000-000000000000")]
    public void InvalidSidCannotTriggerCurrentSessionLogout(string sid) =>
        Assert.Throws<InvalidDataException>(() => SessionIdentity.ReadSessionId(Token(new Claim("sid", sid))));

    [Test]
    public void MissingOrAmbiguousSidIsRejected()
    {
        Assert.Throws<InvalidDataException>(() => SessionIdentity.ReadSessionId(Token(new Claim("deviceInfo", "phone"))));
        Assert.Throws<InvalidDataException>(() => SessionIdentity.ReadSessionId(Token(
            new Claim("sid", Guid.NewGuid().ToString()), new Claim("sid", Guid.NewGuid().ToString()))));
    }

    private static string Token(params Claim[] claims) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims));
}
