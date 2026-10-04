using Govor.Mobile.Models.Responses;
namespace Govor.Mobile.Models.Reactions;

public class ReactionDefinition
{
    public Guid Id { get; set; }
    public Guid PackId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Kind { get; set; }
    public string? Emoji { get; set; }
    public Guid? MediaFileId { get; set; }
    public string? MediaUrl { get; set; }
    public bool IsEnabled { get; set; }
    public string DisplayText => Emoji ?? Code;
}
public class ReactionPack
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ShareCode { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public List<ReactionDefinition> Reactions { get; set; } = new();
}
public class ReactionCount
{
    public Guid? ReactionId { get; set; }
    public string ReactionCode { get; set; } = "";
    public int Count { get; set; }
}
public class MessageReactionsChangedResponse
{
    public Guid MessageId { get; set; }
    public Guid RecipientId { get; set; }
    public int RecipientType { get; set; }
    public long Version { get; set; }
    public Guid ActorId { get; set; }
    public MessageReactionResponse? ActorReaction { get; set; }
    public List<ReactionCount> Counts { get; set; } = new();
}
public class ChannelReactionPolicy
{
    public Guid GroupId { get; set; }
    public int Mode { get; set; }
    public long Version { get; set; }
    public List<Guid> ReactionIds { get; set; } = new();
    public bool Allows(Guid id) => Mode == 0 || (Mode == 2 && ReactionIds.Contains(id));
}
// Counts and the current user's choice have separate watermarks: another actor's
// snapshot may arrive before our command acknowledgement of the same version.
public class MessageReactionState
{
    public Guid MessageId { get; set; }
    public long Version { get; set; } = -1;
    public long OwnVersion { get; set; } = -1;
    public Guid? OwnReactionId { get; set; }
    public string? OwnReactionCode { get; set; }
    public List<ReactionCount> Counts { get; set; } = new();
    public bool Apply(MessageReactionsChangedResponse snapshot, Guid currentUserId)
    {
        if (snapshot.MessageId != MessageId) return false;
        var changed = false;
        if (snapshot.Version > Version)
        {
            Version = snapshot.Version;
            Counts = snapshot.Counts.Where(c => c.Count > 0).ToList();
            changed = true;
        }
        if (snapshot.ActorId == currentUserId && snapshot.Version >= OwnVersion)
        {
            OwnVersion = snapshot.Version;
            OwnReactionId = snapshot.ActorReaction?.ReactionId;
            OwnReactionCode = snapshot.ActorReaction?.ReactionCode;
            changed = true;
        }
        return changed;
    }
}
public class ReactionPackCache
{
    public DateTime UpdatedAt { get; set; }
    public List<ReactionPack> Packs { get; set; } = new();
}
