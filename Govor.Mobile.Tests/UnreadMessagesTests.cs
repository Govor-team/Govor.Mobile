using System.Net;
using System.Reflection;
using Govor.Mobile.Data;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Govor.Mobile.Tests;

public class UnreadMessagesTests
{
    // Only events and unread-count GET are exercised; unused operations fail the test.
    public class EventProxy : DispatchProxy
    {
        private readonly Dictionary<string, Delegate?> _events = new();
        public Func<string, object?[], object?>? Call { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var name = method!.Name;
            args ??= [];
            if (name.StartsWith("add_") || name.StartsWith("remove_"))
            {
                var key = name[(name.IndexOf('_') + 1)..];
                _events.TryGetValue(key, out var previous);
                _events[key] = name.StartsWith("add_") ? Delegate.Combine(previous, (Delegate)args[0]!)
                    : Delegate.Remove(previous, (Delegate)args[0]!);
                return null;
            }
            return Call?.Invoke(name, args) ?? throw new NotSupportedException(name);
        }
        public void Emit(string name, params object[] args)
        { if (_events.TryGetValue(name, out var handler)) handler?.DynamicInvoke(args); }
    }

    private sealed class Session : IJwtProviderService
    {
        public Guid? CurrentUserId { get; set; } = Guid.NewGuid();
        public bool HasRefreshToken => true;
        public event Action? WasClearTokens;
        public Task<string> GetAccessTokenAsync() => Task.FromResult("unused");
        public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetAccessTokenAsync();
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
        public Task ClearAsync() { CurrentUserId = null; WasClearTokens?.Invoke(); return Task.CompletedTask; }
    }

    private sealed class Fixture : IDisposable
    {
        public Session Session { get; } = new();
        public IMessagesRepository Messages { get; } = DispatchProxy.Create<IMessagesRepository, EventProxy>();
        public IChatHub Hub { get; } = DispatchProxy.Create<IChatHub, EventProxy>();
        public IApiClient Api { get; } = DispatchProxy.Create<IApiClient, EventProxy>();
        public UnreadMessagesService Service { get; }
        public int Count { get; set; }
        public int Requests { get; private set; }
        public string? Path { get; private set; }
        public bool Offline { get; set; }
        public Fixture()
        {
            ((EventProxy)Api).Call = (_, args) =>
            {
                Requests++; Path = (string)args[0]!;
                return Task.FromResult(Offline
                    ? new HttpResult<UnreadCountResponse>("Offline", HttpStatusCode.ServiceUnavailable)
                    : HttpResult<UnreadCountResponse>.Success(new() { UnreadCount = Count }));
            };
            Service = NewService();
        }
        public UnreadMessagesService NewService() => new(Messages, Session, Hub, Api,
            new LocalAccountCache(Session, NullLogger<LocalAccountCache>.Instance),
            NullLogger<UnreadMessagesService>.Instance);
        public void Dispose() => Service.Dispose();
    }

    [Test]
    public async Task GroupCountComesFromApiIncludingUnloadedHistory()
    {
        using var fixture = new Fixture { Count = 137 };
        var chat = Guid.NewGuid();
        Assert.That(await fixture.Service.InitializeAsync(chat, true), Is.EqualTo(137));
        Assert.That(fixture.Path, Is.EqualTo($"api/chats/{chat}/unread-count?recipientType=1"));
    }

    [Test]
    public async Task IncomingBurstIsCoalescedWithoutCountingOwnMessagesOrDuplicates()
    {
        using var fixture = new Fixture { Count = 3 };
        var chat = Guid.NewGuid();
        await fixture.Service.InitializeAsync(chat, true);
        fixture.Count = 4;
        var updated = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Service.UnreadCountChanged += (_, _, count) => updated.TrySetResult(count);
        var incoming = new MessageResponse { Id = Guid.NewGuid(), RecipientId = chat,
            RecipientType = RecipientType.Group, SenderId = Guid.NewGuid() };
        var events = (EventProxy)fixture.Messages;
        for (var i = 0; i < 20; i++) events.Emit("OnNewMessage", incoming);
        events.Emit("OnNewMessage", new MessageResponse { Id = Guid.NewGuid(), RecipientId = chat,
            RecipientType = RecipientType.Group, SenderId = fixture.Session.CurrentUserId!.Value });
        Assert.That(await updated.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(4));
        Assert.That(fixture.Requests, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OnlyCurrentReadersReceiptsRefreshBadgeAndOldSnapshotsCannotRestoreUnread(bool bulkRead)
    {
        using var fixture = new Fixture { Count = 2 };
        var chat = Guid.NewGuid();
        await fixture.Service.InitializeAsync(chat, true);
        ((EventProxy)fixture.Messages).Emit("OnMessageViewed", new MessageView {
            ChatId = chat, MessageId = Guid.NewGuid(), UserId = Guid.NewGuid(), RecipientType = RecipientType.Group });
        Assert.That(fixture.Requests, Is.EqualTo(1));
        fixture.Count = 0;
        var updated = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Service.UnreadCountChanged += (_, _, count) => updated.TrySetResult(count);
        if (bulkRead)
            ((EventProxy)fixture.Hub).Emit("ChatRead", new ChatReadResponse {
                ChatId = chat, RecipientType = RecipientType.Group,
                ReaderId = fixture.Session.CurrentUserId!.Value, UnreadCount = 99 });
        else
        {
            var view = new MessageView { ChatId = chat, MessageId = Guid.NewGuid(),
                UserId = fixture.Session.CurrentUserId!.Value, RecipientType = RecipientType.Group };
            ((EventProxy)fixture.Messages).Emit("OnMessageViewed", view);
            ((EventProxy)fixture.Messages).Emit("OnMessageViewed", view);
        }
        Assert.That(await updated.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.Zero);
        Assert.That(fixture.Requests, Is.EqualTo(2));
    }

    [Test]
    public async Task ReconnectionRestoresServerCountForTrackedGroups()
    {
        using var fixture = new Fixture { Count = 7 };
        await fixture.Service.InitializeAsync(Guid.NewGuid(), true);
        fixture.Count = 3;
        var updated = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Service.UnreadCountChanged += (_, _, count) => updated.TrySetResult(count);
        ((EventProxy)fixture.Hub).Emit("Reconnected");
        Assert.That(await updated.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(3));
    }

    [Test]
    public async Task CountSurvivesOfflineRestartWithoutLeakingBetweenAccounts()
    {
        using var fixture = new Fixture { Count = 9 };
        var chat = Guid.NewGuid();
        await fixture.Service.InitializeAsync(chat, true);
        fixture.Service.Dispose();
        fixture.Offline = true;
        using var restarted = fixture.NewService();
        var requests = fixture.Requests;
        Assert.That(await restarted.GetCachedCountAsync(chat, true), Is.EqualTo(9));
        Assert.That(fixture.Requests, Is.EqualTo(requests), "Cached rows must display before the network request.");
        Assert.That(await restarted.InitializeAsync(chat, true), Is.EqualTo(9));
        await fixture.Session.ClearAsync();
        fixture.Session.CurrentUserId = Guid.NewGuid();
        Assert.That(await restarted.InitializeAsync(chat, true), Is.Zero);
    }

    [Test]
    public async Task ResponseFromPreviousAccountDoesNotUpdateCurrentBadge()
    {
        using var fixture = new Fixture();
        var response = new TaskCompletionSource<HttpResult<UnreadCountResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ((EventProxy)fixture.Api).Call = (_, _) => { started.SetResult(); return response.Task; };
        var counts = new List<int>();
        fixture.Service.UnreadCountChanged += (_, _, count) => counts.Add(count);
        var refresh = fixture.Service.InitializeAsync(Guid.NewGuid(), true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await fixture.Session.ClearAsync();
        fixture.Session.CurrentUserId = Guid.NewGuid();
        response.SetResult(HttpResult<UnreadCountResponse>.Success(new() { UnreadCount = 7 }));
        Assert.That(await refresh, Is.Zero);
        Assert.That(counts, Is.Empty);
    }
}
