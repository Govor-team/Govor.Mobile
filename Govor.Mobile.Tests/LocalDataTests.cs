using Govor.Mobile.Data;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Govor.Mobile.Tests;

public class LocalDataTests
{
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

    [Test]
    public async Task MessagesSurviveContextRecreationAndAreIsolatedAcrossAccounts()
    {
        var session = new Session();
        var firstUser = session.CurrentUserId;
        var factory = new AccountDbContextFactory(session);
        var messageId = Guid.NewGuid();
        await using (var context = factory.CreateDbContext())
        {
            await context.Database.MigrateAsync();
            context.Messages.Add(new LocalMessage
            {
                Id = messageId, ChatId = Guid.NewGuid(), SenderId = firstUser!.Value,
                EncryptedContent = "Локальное сообщение", SentAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        session.CurrentUserId = Guid.NewGuid();
        await using (var otherAccount = factory.CreateDbContext())
        {
            await otherAccount.Database.MigrateAsync();
            Assert.That(await otherAccount.Messages.CountAsync(), Is.Zero);
        }
        session.CurrentUserId = firstUser;
        await using var restored = factory.CreateDbContext();
        Assert.That((await restored.Messages.SingleAsync()).Id, Is.EqualTo(messageId));
    }

    [Test]
    public async Task ReadViewsAddedToJsonListArePersisted()
    {
        var factory = new AccountDbContextFactory(new Session());
        await using (var context = factory.CreateDbContext())
        {
            await context.Database.MigrateAsync();
            context.Messages.Add(new LocalMessage
            {
                Id = Guid.NewGuid(), ChatId = Guid.NewGuid(), SenderId = Guid.NewGuid(),
                EncryptedContent = "Прочитано", SentAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            var message = await context.Messages.SingleAsync();
            message.MessageViews.Add(new MessageView { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), MessageId = message.Id });
            await context.SaveChangesAsync();
        }
        await using var restored = factory.CreateDbContext();
        Assert.That((await restored.Messages.SingleAsync()).MessageViews, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ProfileAndFriendsCacheSurvivesRestartWithoutCrossAccountReads()
    {
        var session = new Session();
        var firstUser = session.CurrentUserId;
        var cache = new LocalAccountCache(session, NullLogger<LocalAccountCache>.Instance);
        await cache.WriteAsync("profile", new[] { "Артемий" });
        var restarted = new LocalAccountCache(session, NullLogger<LocalAccountCache>.Instance);
        Assert.That(await restarted.ReadAsync<string[]>("profile"), Is.EqualTo(new[] { "Артемий" }));
        session.CurrentUserId = Guid.NewGuid();
        Assert.That(await restarted.ReadAsync<string[]>("profile"), Is.Null);
        session.CurrentUserId = firstUser;
        Assert.That(await restarted.ReadAsync<string[]>("profile"), Is.EqualTo(new[] { "Артемий" }));
    }
    [Test]
    public async Task InstalledReactionPacksAndSnapshotsSurviveRestartPerAccount()
    {
        var session = new Session();
        var account = session.CurrentUserId;
        var cache = new LocalAccountCache(session, NullLogger<LocalAccountCache>.Instance);
        var packId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        await cache.WriteAsync("reaction-packs", new Govor.Mobile.Models.Reactions.ReactionPackCache
        {
            UpdatedAt = DateTime.UtcNow,
            Packs = new() { new() { Id = packId, Name = "Cats", IsEnabled = true,
                Reactions = new() { new() { Id = Guid.NewGuid(), Emoji = "😺", Kind = 0, IsEnabled = true } } } }
        });
        await cache.WriteAsync($"reaction-{messageId:N}", new Govor.Mobile.Models.Reactions.MessageReactionState
            { MessageId = messageId, Version = 9, OwnVersion = 9, OwnReactionId = packId });
        var restarted = new LocalAccountCache(session, NullLogger<LocalAccountCache>.Instance);
        Assert.That((await restarted.ReadAsync<Govor.Mobile.Models.Reactions.ReactionPackCache>("reaction-packs"))!.Packs.Single().Id, Is.EqualTo(packId));
        Assert.That((await restarted.ReadAsync<Govor.Mobile.Models.Reactions.MessageReactionState>($"reaction-{messageId:N}"))!.Version, Is.EqualTo(9));
        session.CurrentUserId = Guid.NewGuid();
        Assert.That(await restarted.ReadAsync<Govor.Mobile.Models.Reactions.ReactionPackCache>("reaction-packs"), Is.Null);
        session.CurrentUserId = account;
        Assert.That((await restarted.ReadAsync<Govor.Mobile.Models.Reactions.ReactionPackCache>("reaction-packs"))!.Packs.Single().Reactions.Single().Emoji, Is.EqualTo("😺"));
    }
}

