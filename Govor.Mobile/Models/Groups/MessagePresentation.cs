namespace Govor.Mobile.Models.Groups;

/// <summary>Visual direction is independent from the author's ownership and permissions.</summary>
public readonly record struct MessagePresentation(bool Incoming, bool ShowAuthors)
{
    public static MessagePresentation Resolve(bool isGroup, bool isChannel, bool isOwn) =>
        new(isChannel || !isOwn, isGroup && !isChannel);
}
