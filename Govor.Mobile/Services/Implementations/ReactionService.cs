using Govor.Mobile.Models.Reactions;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
namespace Govor.Mobile.Services.Implementations;

public sealed class ReactionService
{
    private readonly IApiClient _api;
    private readonly IChatHub _hub;
    private readonly IJwtProviderService _session;
    private readonly LocalAccountCache _cache;
    private readonly ILogger<ReactionService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _catalogGate = new(1, 1);
    private readonly SemaphoreSlim _mediaGate = new(1, 1);
    private readonly Dictionary<Guid, MessageReactionState> _states = new();
    private readonly Dictionary<Guid, ReactionDefinition> _definitions = new();
    private readonly Dictionary<Guid, ChannelReactionPolicy> _policies = new();
    public event Action<MessageReactionState>? Changed;
    public event Action? Reconnected;
    public event Action<ChannelReactionPolicy>? PolicyChanged;
    public string? CatalogWarning { get; private set; }

    public ReactionService(IApiClient api, IChatHub hub, IJwtProviderService session,
        LocalAccountCache cache, ILogger<ReactionService> logger)
    {
        _api = api; _hub = hub; _session = session; _cache = cache; _logger = logger;
        hub.ReactionsChanged += snapshot => _ = AcceptSafelyAsync(snapshot);
        hub.ReactionPolicyChanged += policy => _ = AcceptPolicyAsync(policy);
        hub.Reconnected += () => Reconnected?.Invoke();
        session.WasClearTokens += () => { lock (_states) _states.Clear(); lock (_definitions) _definitions.Clear(); lock (_policies) _policies.Clear(); };
    }
    private async Task AcceptSafelyAsync(MessageReactionsChangedResponse snapshot)
    {
        try { await AcceptAsync(snapshot); }
        catch (Exception ex) { _logger.LogWarning(ex, "Unable to store reaction event"); }
    }
    public async Task<MessageReactionState> GetStateAsync(Guid messageId)
    {
        await _gate.WaitAsync();
        try { return await LoadStateAsync(messageId); }
        finally { _gate.Release(); }
    }
    private async Task<MessageReactionState> LoadStateAsync(Guid messageId)
    {
        lock (_states) if (_states.TryGetValue(messageId, out var existing)) return existing;
        var state = await _cache.ReadAsync<MessageReactionState>($"reaction-{messageId:N}")
            ?? new MessageReactionState { MessageId = messageId };
        lock (_states) _states[messageId] = state;
        return state;
    }
    public async Task AcceptAsync(MessageReactionsChangedResponse snapshot)
    {
        if (_session.CurrentUserId is not Guid userId) return;
        await _gate.WaitAsync();
        try
        {
            var state = await LoadStateAsync(snapshot.MessageId);
            if (_session.CurrentUserId != userId || !state.Apply(snapshot, userId)) return;
            await _cache.WriteAsync($"reaction-{state.MessageId:N}", state);
            Changed?.Invoke(state);
        }
        finally { _gate.Release(); }
    }
    public Task AcceptHistoryAsync(MessageResponse message)
    {
        var mine = message.Reactions.FirstOrDefault(r => r.UserId == _session.CurrentUserId);
        return AcceptAsync(new MessageReactionsChangedResponse
        {
            MessageId = message.Id, Version = message.ReactionsVersion,
            ActorId = _session.CurrentUserId ?? Guid.Empty, ActorReaction = mine,
            Counts = message.Reactions.GroupBy(r => (r.ReactionId, r.ReactionCode))
                .Select(g => new ReactionCount { ReactionId = g.Key.ReactionId, ReactionCode = g.Key.ReactionCode, Count = g.Count() }).ToList()
        });
    }
    public async Task RefreshAsync(Guid messageId)
    {
        var result = await _api.GetAsync<MessageReactionsChangedResponse>($"api/messages/{messageId}/reactions");
        if (result.IsSuccess && result.Value is not null) await AcceptAsync(result.Value);
    }
    public async Task ToggleAsync(Guid messageId, Guid reactionId)
    {
        var state = await GetStateAsync(messageId);
        var result = state.OwnReactionId == reactionId
            ? await _hub.RemoveReaction(messageId) : await _hub.React(messageId, reactionId);
        if (result.Status != HubResultStatus.Success || result.Value is null)
        {
            await GetPacksAsync(false, true);
            throw new InvalidOperationException(result.ErrorMessage ?? "Не удалось изменить реакцию. Проверьте подключение.");
        }
        await AcceptAsync(result.Value);
    }
    public async Task RemoveAsync(Guid messageId)
    {
        var result = await _hub.RemoveReaction(messageId);
        if (result.Status != HubResultStatus.Success || result.Value is null)
            throw new InvalidOperationException(result.ErrorMessage ?? "Не удалось убрать реакцию.");
        await AcceptAsync(result.Value);
    }
    public async Task<List<ReactionPack>> GetPacksAsync(bool catalog = false, bool force = false)
    {
        await _catalogGate.WaitAsync();
        var account = _session.CurrentUserId;
        try
        {
            var key = catalog ? "reaction-catalog" : "reaction-packs";
            var saved = await _cache.ReadAsync<ReactionPackCache>(key);
            if (!force && saved != null && DateTime.UtcNow - saved.UpdatedAt < TimeSpan.FromMinutes(5))
            {
                Remember(saved.Packs); return saved.Packs;
            }
            var packs = new List<ReactionPack>();
            for (var skip = 0; ; skip += 100)
            {
                var result = await _api.GetAsync<List<ReactionPack>>($"api/reaction-packs{(catalog ? "" : "/mine")}?skip={skip}&take=100");
                if (!result.IsSuccess || result.Value is null)
                {
                    CatalogWarning = saved is null ? "Не удалось загрузить паки. Проверьте подключение и версию сервера." : "Показаны сохранённые паки — сервер сейчас недоступен.";
                    Remember(saved?.Packs ?? new()); return saved?.Packs ?? new();
                }
                packs.AddRange(result.Value);
                if (result.Value.Count < 100) break;
            }
            if (_session.CurrentUserId != account) return new();
            CatalogWarning = null;
            await _cache.WriteAsync(key, new ReactionPackCache { UpdatedAt = DateTime.UtcNow, Packs = packs });
            Remember(packs);
            return packs;
        }
        finally { _catalogGate.Release(); }
    }
    private void Remember(IEnumerable<ReactionPack> packs)
    {
        lock (_definitions) foreach (var definition in packs.SelectMany(p => p.Reactions)) _definitions[definition.Id] = definition;
    }
    public async Task SubscribeAsync(ReactionPack pack, bool install)
    {
        if (pack.IsDefault) return;
        if (install)
        {
            var result = await _api.PutAsync<object>($"api/reaction-packs/{pack.Id}/subscription", new { });
            if (!result.IsSuccess) throw new InvalidOperationException("Не удалось добавить пак.");
        }
        else
        {
            var result = await _api.DeleteAsync($"api/reaction-packs/{pack.Id}/subscription");
            if (!result.IsSuccess) throw new InvalidOperationException("Не удалось убрать пак.");
        }
        var packs = await _cache.ReadAsync<ReactionPackCache>("reaction-packs") ?? new();
        packs.Packs.RemoveAll(p => p.Id == pack.Id);
        if (install) packs.Packs.Add(pack);
        // An installation is confirmed on the server; preserve it even offline.
        await _cache.WriteAsync("reaction-packs", packs);
        Remember(packs.Packs);
        if (install) foreach (var definition in pack.Reactions.Where(r => r.Kind != 0)) await GetMediaPathAsync(definition);
    }
    public async Task<ReactionPack> GetSharedAsync(string shareCode)
    {
        var result = await _api.GetAsync<ReactionPack>($"api/reaction-packs/shared/{Uri.EscapeDataString(shareCode.Trim())}");
        if (!result.IsSuccess || result.Value is null) throw new InvalidOperationException("Пак с таким кодом не найден.");
        Remember(new[] { result.Value }); return result.Value;
    }
    public async Task<ReactionDefinition?> GetDefinitionAsync(Guid id)
    {
        lock (_definitions) if (_definitions.TryGetValue(id, out var known)) return known;
        var saved = await _cache.ReadAsync<ReactionDefinition>($"reaction-definition-{id:N}");
        if (saved != null) { lock (_definitions) _definitions[id] = saved; return saved; }
        var result = await _api.GetAsync<ReactionDefinition>($"api/reactions/{id}");
        if (!result.IsSuccess || result.Value is null) return null;
        await _cache.WriteAsync($"reaction-definition-{id:N}", result.Value);
        lock (_definitions) _definitions[id] = result.Value;
        return result.Value;
    }
    public async Task<string?> GetMediaPathAsync(ReactionDefinition definition)
    {
        if (definition.MediaFileId is not Guid fileId || _session.CurrentUserId is not Guid userId) return null;
        var directory = Path.Combine(FileSystem.AppDataDirectory, $"{userId:N}-reaction-media");
        var path = Path.Combine(directory, $"{fileId:N}.{(definition.Kind == 2 ? "gif" : "png")}");
        await _mediaGate.WaitAsync();
        try
        {
            if (File.Exists(path)) return path;
            // Immutable media IDs are downloaded once, through the authenticated API client.
            var result = await _api.GetFileStreamAsync($"api/media/download/{fileId}");
            if (!result.IsSuccess || result.Value is null) return null;
            await using var input = result.Value.Stream;
            Directory.CreateDirectory(directory);
            var temporary = path + ".tmp";
            await using (var output = File.Create(temporary)) await input.CopyToAsync(output);
            File.Move(temporary, path, true);
            return path;
        }
        catch (IOException ex) { _logger.LogWarning(ex, "Unable to cache reaction media"); return null; }
        finally { _mediaGate.Release(); }
    }
    public async Task<ChannelReactionPolicy?> GetPolicyAsync(Guid groupId)
    {
        var result = await _api.GetAsync<ChannelReactionPolicy>($"api/groups/{groupId}/reactions");
        if (result.IsSuccess && result.Value != null) await AcceptPolicyAsync(result.Value);
        lock (_policies) if (_policies.TryGetValue(groupId, out var policy)) return policy;
        return await _cache.ReadAsync<ChannelReactionPolicy>($"reaction-policy-{groupId:N}");
    }
    private async Task AcceptPolicyAsync(ChannelReactionPolicy policy)
    {
        lock (_policies)
        {
            if (_policies.TryGetValue(policy.GroupId, out var old) && old.Version > policy.Version) return;
            _policies[policy.GroupId] = policy;
        }
        await _cache.WriteAsync($"reaction-policy-{policy.GroupId:N}", policy);
        PolicyChanged?.Invoke(policy);
    }
}
