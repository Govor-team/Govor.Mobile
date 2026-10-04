using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Utilities;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

/// <summary>Hub payloads invalidate HTTP state; they never grant client permissions.</summary>
public sealed class GroupRealtimeService(IChatHub hub, ILogger<GroupRealtimeService> logger)
{
    public IDisposable Subscribe(Guid? groupId, Func<bool, Task> refresh)
    {
        var queue = new CoalescingRefresh(
            reconnect => MainThread.InvokeOnMainThreadAsync(() => refresh(reconnect)),
            ex => logger.LogWarning(ex, "Could not refresh community {GroupId}", groupId));
        void Profile(Models.Groups.GroupProfileChangedResponse change)
        { if (groupId == null || change.GroupId == groupId) queue.Request(); }
        void Member(Models.Groups.GroupMemberChangedResponse change)
        { if (groupId == null || change.GroupId == groupId) queue.Request(); }
        void Reconnect() => queue.Request(true);
        hub.GroupProfileChanged += Profile;
        hub.GroupMemberChanged += Member;
        hub.Reconnected += Reconnect;
        return new Subscription(() =>
        {
            queue.Dispose();
            hub.GroupProfileChanged -= Profile;
            hub.GroupMemberChanged -= Member;
            hub.Reconnected -= Reconnect;
        });
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        private Action? _unsubscribe = unsubscribe;
    }
}
