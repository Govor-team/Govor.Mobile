using System.Text.Json;
using Govor.Mobile.Models.Groups;
using Govor.Mobile.Utilities;

namespace Govor.Mobile.Tests;

public class GroupRealtimeTests
{
    [Test]
    public void MembershipEventAcceptsLossOfRole()
    {
        var id = Guid.NewGuid();
        var change = JsonSerializer.Deserialize<GroupMemberChangedResponse>(
            $$"""{"groupId":"{{id}}","userId":"{{Guid.NewGuid()}}","status":1,"role":null}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Multiple(() =>
        {
            Assert.That(change.GroupId, Is.EqualTo(id));
            Assert.That(change.Status, Is.EqualTo(GroupMemberStatus.Banned));
            Assert.That(change.Role, Is.Null);
        });
    }

    [Test]
    public async Task NotificationsDuringRefreshAreNotLostAndReconnectIsPreserved()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var concurrent = 0;
        var maximum = 0;
        using var queue = new CoalescingRefresh(async reconnect =>
        {
            maximum = Math.Max(maximum, Interlocked.Increment(ref concurrent));
            var call = Interlocked.Increment(ref calls);
            if (call == 1) { firstStarted.SetResult(); await releaseFirst.Task; }
            Interlocked.Decrement(ref concurrent);
            if (call == 2) secondFinished.SetResult(reconnect);
        }, ex => secondFinished.TrySetException(ex));
        queue.Request();
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Request(); queue.Request(true); queue.Request();
        releaseFirst.SetResult();
        Assert.That(await secondFinished.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(calls, Is.EqualTo(2));
        Assert.That(maximum, Is.EqualTo(1));
    }

    [Test]
    public async Task DisappearingScreenCancelsItsPendingPass()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var queue = new CoalescingRefresh(async _ =>
        { Interlocked.Increment(ref calls); started.TrySetResult(); await release.Task; }, _ => { });
        queue.Request();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        queue.Request(true); queue.Dispose(); queue.Request(); release.SetResult();
        await Task.Delay(180);
        Assert.That(calls, Is.EqualTo(1));
    }
}
