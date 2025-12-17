namespace Govor.Mobile.Services.Hubs;

public interface IProfileHubService : IHubClient 
{
    Task<HubResult<bool>> SetDescriptionAsync(string description);
    Task<HubResult<bool>> SetAvatarAsync(Guid iconId);
    
    public event Action<Guid, DescriptionUpdatePayload>? OnDescriptionUpdated;
    public event Action<Guid, AvatarUpdatePayload>? OnAvatarUpdated;
}

public class DescriptionUpdatePayload
{
    public Guid UserId { get; set; }
    public string? Description { get; set; }
}

public class AvatarUpdatePayload
{
    public Guid UserId { get; set; }
    public Guid IconId { get; set; }
}