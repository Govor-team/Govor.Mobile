using Govor.Mobile.Models.Groups;

namespace Govor.Mobile.Tests;

public class MemberContactTests
{
    [Test]
    public void ExistingFriendOpensConversationInsteadOfSendingAnotherRequest()
    {
        var state = MemberContactState.Resolve(false, true, Guid.NewGuid(), true);
        Assert.That(state.Kind, Is.EqualTo(MemberContactKind.Friend));
        Assert.That(state.ActionTitle, Is.EqualTo("Написать сообщение"));
        Assert.That(state.CanAct, Is.True);
    }

    [Test]
    public void PendingOutgoingRequestCannotBeSentAgain()
    {
        var state = MemberContactState.Resolve(false, false, null, true);
        Assert.That(state.Kind, Is.EqualTo(MemberContactKind.OutgoingRequest));
        Assert.That(state.CanAct, Is.False);
    }

    [Test]
    public void IncomingRequestOffersAcceptanceAndPreservesItsId()
    {
        var id = Guid.NewGuid();
        var state = MemberContactState.Resolve(false, false, id, true);
        Assert.That(state.Kind, Is.EqualTo(MemberContactKind.IncomingRequest));
        Assert.That(state.RequestId, Is.EqualTo(id));
        Assert.That(state.CanAct, Is.True);
    }

    [Test]
    public void NewContactCanReceiveRequestButSelfCannot()
    {
        Assert.That(MemberContactState.Resolve(false, false, null, false).Kind, Is.EqualTo(MemberContactKind.NewContact));
        Assert.That(MemberContactState.Resolve(true, true, Guid.NewGuid(), true).CanAct, Is.False);
    }
}
