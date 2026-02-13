namespace Govor.Mobile.Models;

public record MediaReference
{
    public Guid MediaId { get; init; }
    public string EncryptedKey { get; init; } = string.Empty;
}