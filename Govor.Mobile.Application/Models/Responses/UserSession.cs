namespace Govor.Mobile.Application.Models.Responses;

public record UserSession(Guid id, string deviceInfo, DateTime createdAt, DateTime expiresAt, bool isRevoked);
