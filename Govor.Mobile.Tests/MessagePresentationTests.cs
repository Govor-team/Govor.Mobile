using Govor.Mobile.Models.Groups;

namespace Govor.Mobile.Tests;

public class MessagePresentationTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void ChannelPostsAlwaysAppearIncomingAndAnonymous(bool own)
    {
        var presentation = MessagePresentation.Resolve(true, true, own);
        Assert.That(presentation.Incoming, Is.True);
        Assert.That(presentation.ShowAuthors, Is.False);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void GroupsKeepAuthorIdentityAndOwnMessageDirection(bool own, bool incoming)
    {
        var presentation = MessagePresentation.Resolve(true, false, own);
        Assert.That(presentation.Incoming, Is.EqualTo(incoming));
        Assert.That(presentation.ShowAuthors, Is.True);
    }

    [Test]
    public void PrivateChatsDoNotGetGroupAuthorLabels() =>
        Assert.That(MessagePresentation.Resolve(false, false, false).ShowAuthors, Is.False);
}
