namespace Govor.Mobile.Models.Responses;

public record UserSession(Guid id, string deviceInfo, DateTime createdAt, DateTime expiresAt, bool isRevoked);
