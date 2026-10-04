using System.Text.Json;
using Govor.Mobile.Models.Reactions;
using Govor.Mobile.Models.Responses;
namespace Govor.Mobile.Tests;
public class ReactionStateTests
{
    private readonly Guid _me = Guid.NewGuid(), _other = Guid.NewGuid(), _reaction = Guid.NewGuid();
    private MessageReactionsChangedResponse Snapshot(Guid message, long version, Guid actor, bool removed = false, int count = 1) => new()
    {
        MessageId = message, Version = version, ActorId = actor,
        ActorReaction = removed ? null : new MessageReactionResponse { ReactionId = _reaction, ReactionCode = "👍", UserId = actor },
        Counts = count == 0 ? new() : new() { new() { ReactionId = _reaction, ReactionCode = "👍", Count = count } }
    };
    [Test]
    public void OlderSnapshotCannotOverwriteNewerCounts()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 4, _other, count: 3), _me);
        state.Apply(Snapshot(state.MessageId, 2, _other, count: 1), _me);
        Assert.That(state.Counts.Single().Count, Is.EqualTo(3));
    }
    [Test]
    public void OtherActorsReactionIsNeverShownAsMyChoice()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 1, _other), _me);
        Assert.That(state.OwnReactionId, Is.Null);
    }
    [Test]
    public void CommandAcknowledgementCanFillOwnChoiceAfterSameVersionEvent()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 2, _other, count: 2), _me);
        state.Apply(Snapshot(state.MessageId, 2, _me, count: 2), _me);
        Assert.That(state.OwnReactionId, Is.EqualTo(_reaction));
        Assert.That(state.Counts.Single().Count, Is.EqualTo(2));
    }
    [Test]
    public void DelayedCommandCannotRestoreRemovedOwnReaction()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 4, _me, removed: true, count: 0), _me);
        state.Apply(Snapshot(state.MessageId, 3, _me), _me);
        Assert.That(state.OwnReactionId, Is.Null);
        Assert.That(state.Counts, Is.Empty);
    }
    [Test]
    public void SnapshotsReplaceCountsInsteadOfAddingDuplicates()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 1, _me), _me);
        state.Apply(Snapshot(state.MessageId, 1, _me), _me);
        Assert.That(state.Counts.Single().Count, Is.EqualTo(1));
    }
    [Test]
    public void PersistedStateRetainsVersionAndOwnChoiceAfterRestart()
    {
        var state = new MessageReactionState { MessageId = Guid.NewGuid() };
        state.Apply(Snapshot(state.MessageId, 5, _me), _me);
        var restored = JsonSerializer.Deserialize<MessageReactionState>(JsonSerializer.Serialize(state))!;
        restored.Apply(Snapshot(state.MessageId, 4, _other, count: 20), _me);
        Assert.That(restored.Version, Is.EqualTo(5));
        Assert.That(restored.OwnReactionId, Is.EqualTo(_reaction));
        Assert.That(restored.Counts.Single().Count, Is.EqualTo(1));
    }
    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(2, true)]
    public void ChannelPolicyControlsWhichReactionsCanBeChosen(int mode, bool allowed)
    {
        var policy = new ChannelReactionPolicy { Mode = mode, ReactionIds = new() { _reaction } };
        Assert.That(policy.Allows(_reaction), Is.EqualTo(allowed));
        if (mode == 2) Assert.That(policy.Allows(Guid.NewGuid()), Is.False);
    }
}
