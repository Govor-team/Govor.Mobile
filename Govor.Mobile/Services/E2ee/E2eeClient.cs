using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Security;

namespace Govor.Mobile.Services.E2ee;

public interface IE2eeStorage
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}

public sealed class E2eeClient(IApiClient api, IJwtProviderService jwt, IServerIpProvider server,
    IE2eeStorage storage, IE2eeRecoveryPrompt? recoveryPrompt = null, TimeProvider? clock = null,
    PasswordAccountClient? passwords = null)
{
    private readonly SemaphoreSlim _keyLock = new(1, 1);
    private readonly SemaphoreSlim _trustLock = new(1, 1);
    private readonly SemaphoreSlim _decryptLock = new(1, 1);
    private string? _registered;

    private string Scope(Guid user) => "govor-e2ee-v1-" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new Uri(server.IP).GetLeftPart(UriPartial.Authority)))) + "-" + user.ToString("N");

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly E2eeRecoveryClient _recovery = new(api, storage, recoveryPrompt, passwords);
    private LocalRecovery? _cachedRecovery;
    private string? _cachedScope;
    private DateTimeOffset _nextSync;
    private bool _archiveDirty;
    private sealed record ReadyState(DevicePrivateKeys Private, DeviceKey Public, string Scope,
        SavedDevice[] Devices, LocalRecovery Recovery);

    private async Task<ReadyState> ReadyAsync(bool forceSync = false)
    {
        await _keyLock.WaitAsync();
        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(await jwt.GetAccessTokenAsync());
            var user = Guid.Parse(token.Claims.Single(c => c.Type == "userId").Value);
            var session = Guid.Parse(token.Claims.Single(c => c.Type == "sid").Value);
            var scope = Scope(user);
            await CompletePendingResetAsync(user, scope);
            var encoded = await storage.GetAsync(scope + "-sessions");
            var sessions = encoded is null ? new List<Guid>() :
                JsonSerializer.Deserialize<List<Guid>>(encoded, E2eeProtocol.Json)
                    ?? throw new CryptographicException("Не удалось загрузить список ключей E2EE.");
            var devices = new List<SavedDevice>();
            foreach (var savedSession in sessions.Distinct())
            {
                var value = await storage.GetAsync($"{scope}-key-{savedSession:N}")
                    ?? throw new CryptographicException("Архив ключей E2EE повреждён.");
                devices.Add(JsonSerializer.Deserialize<SavedDevice>(value, E2eeProtocol.Json)
                    ?? throw new CryptographicException("Не удалось загрузить ключ E2EE."));
            }
            var saved = devices.SingleOrDefault(d => d.SessionId == session);
            var now = _clock.GetUtcNow();
            if (saved is null)
            {
                var keys = E2eeProtocol.GenerateKeys();
                saved = new(session, keys, E2eeProtocol.PublicKey(keys, user, session).Fingerprint,
                    RotatedAt: now.ToUnixTimeSeconds());
                devices.Add(saved);
                await storage.SetAsync($"{scope}-key-{session:N}", JsonSerializer.Serialize(saved));
                sessions.Add(session);
                await storage.SetAsync(scope + "-sessions", JsonSerializer.Serialize(sessions));
                forceSync = true;
            }
            // Upgrade v1's local records without forcing an immediate rotation.
            if (saved.RotatedAt == 0)
            {
                saved = saved with { RotatedAt = now.ToUnixTimeSeconds() };
                await storage.SetAsync($"{scope}-key-{session:N}", JsonSerializer.Serialize(saved));
            }
            var pendingJson = await storage.GetAsync($"{scope}-pending-{session:N}");
            var pending = pendingJson is null ? null : JsonSerializer.Deserialize<SavedDevice>(pendingJson, E2eeProtocol.Json);
            if (pending is null && now.ToUnixTimeSeconds() - saved.RotatedAt >= 7 * 86400)
            {
                var rotated = E2eeProtocol.GenerateKeys();
                var keys = new DevicePrivateKeys(saved.Keys.IdentityKey, rotated.EncryptionKey);
                pending = new(session, keys, E2eeProtocol.PublicKey(keys, user, session).Fingerprint,
                    saved.Generation + 1, saved.Fingerprint, now.ToUnixTimeSeconds());
                // Journal before upload/activation; retries reuse exactly the same key.
                await storage.SetAsync($"{scope}-pending-{session:N}", JsonSerializer.Serialize(pending));
            }
            var candidate = pending ?? saved;
            if (forceSync || pending is not null || _archiveDirty || _cachedScope != scope || _cachedRecovery is null || now >= _nextSync)
            {
                var allKeys = devices.Append(candidate).Select(d => new HistoryKey(
                    E2eeProtocol.PublicKey(d.Keys, user, d.SessionId).IdentityKey, d.Keys.EncryptionKey)).ToArray();
                _cachedRecovery = await _recovery.SynchronizeAsync(user, scope, allKeys);
                _cachedScope = scope;
                _nextSync = now.AddMinutes(1);
                _archiveDirty = false;
            }
            var publicKey = E2eeRecoveryProtocol.Certify(E2eeProtocol.PublicKey(candidate.Keys, user, session),
                _cachedRecovery!.Archive.IdentityPrivateKey, candidate.Generation, candidate.PreviousFingerprint);
            var registration = scope + "-" + session + "-" + publicKey.Fingerprint;
            if (_registered != registration)
            {
                var result = await api.PutAsync<JsonElement>("api/e2ee/device", publicKey);
                if (!result.IsSuccess)
                    throw new InvalidOperationException("Не удалось зарегистрировать или обновить ключи E2EE. Повторите попытку.");
                // A certificate can be added to a revoked v1 session for verifying
                // historical signatures, without reactivating it as a recipient.
                var historical = await api.GetAsync<DeviceKey[]>("api/e2ee/devices");
                if (!historical.IsSuccess || historical.Value is null)
                    throw new InvalidOperationException("Не удалось проверить архив устройств E2EE.");
                foreach (var legacy in historical.Value.Where(d => d.AccountIdentityKey is null &&
                             _cachedRecovery.Archive.Keys.Any(k => k.Fingerprint == d.Fingerprint)))
                {
                    if (legacy.UserId != user) throw new CryptographicException("Чужое устройство в архиве аккаунта.");
                    var certificate = E2eeRecoveryProtocol.Certify(legacy, _cachedRecovery.Archive.IdentityPrivateKey,
                        legacy.Generation, legacy.PreviousFingerprint);
                    var certified = await api.PutAsync<JsonElement>("api/e2ee/devices/certificate", certificate);
                    if (!certified.IsSuccess) throw new InvalidOperationException("Не удалось подтвердить ключ истории E2EE.");
                }
                _registered = registration;
            }
            if (pending is not null)
            {
                await storage.SetAsync($"{scope}-key-{session:N}", JsonSerializer.Serialize(pending));
                await storage.SetAsync($"{scope}-pending-{session:N}", "null");
                devices.RemoveAll(d => d.SessionId == session);
                devices.Add(pending);
            }
            if (jwt.CurrentUserId != user) throw new UnauthorizedAccessException("Аккаунт изменился.");
            return new(candidate.Keys, publicKey, scope, devices.ToArray(), _cachedRecovery);
        }
        finally { _keyLock.Release(); }
    }
    public async Task InitializeAsync() => await ReadyAsync();
    public async Task<DeviceKey> GetOwnKeyAsync() => (await ReadyAsync()).Public;
    public async Task<string> GetRecoveryCodeAsync() => (await ReadyAsync(forceSync: true)).Recovery.Code;

    public async Task ChangePasswordAsync(string oldPassword, string newPassword)
    {
        var state = await ReadyAsync(forceSync: true);
        await (passwords ?? throw new InvalidOperationException("Защищённая авторизация недоступна."))
            .ChangeAsync(state.Public.UserId, state.Recovery, oldPassword, newPassword);
    }

    private sealed record ResetJournal(LocalRecovery Recovery, SavedDevice Device, string ExpectedIdentity);

    public async Task ResetEncryptionAsync()
    {
        if (passwords is null) throw new InvalidOperationException("Защищённая авторизация недоступна.");
        await _keyLock.WaitAsync();
        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(await jwt.GetAccessTokenAsync());
            var user = Guid.Parse(token.Claims.Single(c => c.Type == "userId").Value);
            var session = Guid.Parse(token.Claims.Single(c => c.Type == "sid").Value);
            var scope = Scope(user);
            if (await CompletePendingResetAsync(user, scope)) return;
            var previous = (await api.GetAsync<RecoveryState>("api/e2ee/recovery")).Value?.Vault
                ?? throw new InvalidOperationException("Архив восстановления не найден.");
            var pending = await storage.GetAsync(scope + "-reset-operation");
            var journal = pending is null ? null : JsonSerializer.Deserialize<ResetJournal>(pending, E2eeProtocol.Json);
            if (journal is null || journal.ExpectedIdentity != previous.IdentityKey)
            {
                var keys = E2eeProtocol.GenerateKeys();
                var publicKey = E2eeProtocol.PublicKey(keys, user, session);
                var recovery = new LocalRecovery(E2eeRecoveryProtocol.GenerateCode(), previous.Revision + 1,
                    new(E2eeRecoveryProtocol.GenerateIdentity(), [new(publicKey.IdentityKey, keys.EncryptionKey)], []));
                journal = new(recovery, new(session, keys, publicKey.Fingerprint, RotatedAt: _clock.GetUtcNow().ToUnixTimeSeconds()), previous.IdentityKey);
                await storage.SetAsync(scope + "-reset-operation", JsonSerializer.Serialize(journal));
            }
            await passwords.ResetAsync(user, previous, journal.Recovery);
            await CompletePendingResetAsync(user, scope);
        }
        finally { _keyLock.Release(); }
    }

    private async Task<bool> CompletePendingResetAsync(Guid user, string scope)
    {
        var json = await storage.GetAsync(scope + "-reset-operation");
        if (json is null || json == "null") return false;
        var journal = JsonSerializer.Deserialize<ResetJournal>(json, E2eeProtocol.Json)!;
        var remote = await api.GetAsync<RecoveryState>("api/e2ee/recovery");
        if (!remote.IsSuccess) throw new InvalidOperationException("Не удалось проверить завершение сброса шифрования.");
        var vault = remote.Value?.Vault;
        if (vault?.IdentityKey != E2eeRecoveryProtocol.PublicIdentity(journal.Recovery.Archive.IdentityPrivateKey)) return false;
        // Verify the exact journaled secret against the authenticated remote vault
        // before committing new local keys after an interrupted response.
        E2eeRecoveryProtocol.Open(vault, user, journal.Recovery.Code);
        await storage.SetAsync(scope + "-recovery", JsonSerializer.Serialize(journal.Recovery with { Revision = vault.Revision }));
        await storage.SetAsync($"{scope}-key-{journal.Device.SessionId:N}", JsonSerializer.Serialize(journal.Device));
        await storage.SetAsync($"{scope}-pending-{journal.Device.SessionId:N}", "null");
        await storage.SetAsync(scope + "-sessions", JsonSerializer.Serialize(new[] { journal.Device.SessionId }));
        await storage.SetAsync(scope + "-reset-operation", "null");
        _cachedRecovery = null; _registered = null; _cachedScope = null;
        return true;
    }

    private async Task TrustAsync(DeviceKey device, DeviceKey own, string scope, long? messageCreatedAt = null)
    {
        E2eeProtocol.ValidatePublicKey(device);
        if (device.UserId == own.UserId && device.Fingerprint == own.Fingerprint) return;
        if (device.UserId == own.UserId && device.AccountIdentityKey == own.AccountIdentityKey &&
            device.AccountIdentityKey is not null) return;
        await _trustLock.WaitAsync();
        try
        {
            var identity = device.AccountIdentityKey
                ?? throw new CryptographicException("Участнику нужно обновить приложение и настроить защищённую историю.");
            var pin = $"{scope}-account-{device.UserId:N}";
            var known = _cachedRecovery?.Archive.TrustedAccounts.GetValueOrDefault(device.UserId) ?? await storage.GetAsync(pin);
            if (known == identity) return;
            // Trust on first use is automatic. A signed device certificate keeps
            // this pin stable across logins, devices, and encryption key rotation.
            long epoch = 0;
            if (known is null && passwords is not null)
            {
                var lineage = await api.GetAsync<AccountIdentityChange[]>($"api/e2ee/identities/{device.UserId}/changes");
                if (!lineage.IsSuccess || lineage.Value is null) throw new CryptographicException("Не удалось проверить ключ аккаунта.");
                epoch = lineage.Value.Where(c => c.UserId == device.UserId && c.IdentityKey == identity)
                    .Select(c => c.Epoch).DefaultIfEmpty(0).Max();
            }
            if (known is not null)
            {
                var changes = await api.GetAsync<AccountIdentityChange[]>($"api/e2ee/identities/{device.UserId}/changes");
                if (!changes.IsSuccess || changes.Value is null) throw new CryptographicException("Не удалось проверить смену ключей аккаунта.");
                var current = known;
                foreach (var change in changes.Value.OrderBy(c => c.Epoch))
                {
                    if (change.UserId != device.UserId) throw new CryptographicException("Неверный журнал смены ключей.");
                    if (change.PreviousIdentityKey != current) continue;
                    current = change.IdentityKey; epoch = change.Epoch;
                    if (current == identity) break;
                }
                if (current != identity)
                {
                    // Old signatures remain valid for messages preceding a
                    // recorded identity reset; never use old roots for sending.
                    current = identity;
                    foreach (var change in changes.Value.OrderBy(c => c.Epoch))
                    {
                        if (change.PreviousIdentityKey != current) continue;
                        if (!messageCreatedAt.HasValue || messageCreatedAt.Value > change.ChangedAt)
                            throw new CryptographicException("Старый ключ использован после сброса шифрования.");
                        current = change.IdentityKey;
                        if (current == known) return;
                    }
                    throw new CryptographicException("Ключ аккаунта собеседника изменился без подтверждённого сброса.");
                }
                if (passwords is not null) await passwords.NotifyIdentityChangedAsync(device.UserId);
            }
            await _keyLock.WaitAsync();
            try
            {
                if (_cachedScope != scope || _cachedRecovery is null) throw new UnauthorizedAccessException("Аккаунт изменился.");
                var epochs = new Dictionary<Guid, long>(_cachedRecovery.Archive.IdentityEpochs ?? []);
                epochs[device.UserId] = epoch;
                _cachedRecovery = _cachedRecovery with { Archive = _cachedRecovery.Archive with { IdentityEpochs = epochs } };
                _cachedRecovery.Archive.TrustedAccounts[device.UserId] = identity;
                await storage.SetAsync(scope + "-recovery", JsonSerializer.Serialize(_cachedRecovery));
                await storage.SetAsync(pin, identity);
                _archiveDirty = true;
            }
            finally { _keyLock.Release(); }
        }
        finally { _trustLock.Release(); }
    }

    public async Task<string> EncryptAsync(string text, Guid conversation, int type, Guid? replyTo,
        Guid[] mediaIds, Guid? editedMessage = null)
    {
        var state = await ReadyAsync();
        var result = await api.GetAsync<ConversationKeys>($"api/e2ee/conversations/{type}/{conversation}/keys");
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("У всех участников должны быть ключи E2EE. Попросите их обновить приложение и войти.");
        var directory = result.Value;
        if (directory.ConversationId != conversation || directory.RecipientType != type ||
            !directory.Members.Contains(state.Public.UserId) || directory.Devices.Length is 0 or > E2eeProtocol.MaxDevices ||
            directory.Members.Any(m => !directory.Devices.Any(d => d.UserId == m)) ||
            directory.Devices.Any(d => !directory.Members.Contains(d.UserId)) ||
            !directory.Devices.Any(d => d.UserId == state.Public.UserId && d.Fingerprint == state.Public.Fingerprint))
            throw new CryptographicException("Неверный список устройств участников.");
        // Device certificates chain to the pinned account identity automatically.
        foreach (var device in directory.Devices.DistinctBy(d => (d.UserId, d.Fingerprint)))
            if (device.UserId != state.Public.UserId || !state.Devices.Any(d => d.Fingerprint == device.Fingerprint))
                await TrustAsync(device, state.Public, state.Scope);
        // Group membership is authorized by the existing join/friendship flow.
        // This does not authenticate the membership service against a malicious server.
        var attachments = new List<AttachmentSecret>();
        foreach (var id in mediaIds)
            attachments.Add(await GetAttachmentAsync(id, state.Scope)
                ?? throw new CryptographicException("Отсутствует локальный ключ вложения. Загрузите файл заново."));
        if (jwt.CurrentUserId != state.Public.UserId) throw new UnauthorizedAccessException("Аккаунт изменился.");
        return E2eeProtocol.Encrypt(new(text, attachments.ToArray()), state.Private, state.Public,
            directory, editedMessage ?? Guid.NewGuid(), replyTo, editedMessage.HasValue);
    }

    public async Task<string> DecryptAsync(string content, Guid message, Guid conversation, int type,
        Guid sender, Guid? replyTo, bool? isEdit = null)
    {
        await _decryptLock.WaitAsync();
        try { return await DecryptCoreAsync(content, message, conversation, type, sender, replyTo, isEdit); }
        finally { _decryptLock.Release(); }
    }

    private async Task<string> DecryptCoreAsync(string content, Guid message, Guid conversation, int type,
        Guid sender, Guid? replyTo, bool? isEdit)
    {
        // Retain readable legacy history, with an explicit label. Never send it back as plaintext.
        if (!E2eeProtocol.IsEncrypted(content)) return "[Без E2EE] " + content;
        var state = await ReadyAsync();
        var envelope = E2eeProtocol.Verify(content);
        // Verify context and decrypt before accepting any sender trust state.
        var decryptingKey = state.Recovery.Archive.Keys.FirstOrDefault(k => envelope.Recipients.Contains(k.Fingerprint));
        if (decryptingKey is null)
        {
            state = await ReadyAsync(forceSync: true);
            decryptingKey = state.Recovery.Archive.Keys.FirstOrDefault(k => envelope.Recipients.Contains(k.Fingerprint))
                ?? throw new CryptographicException("В архиве пока нет ключа этого сообщения. Синхронизируйте доверенное устройство.");
        }
        var plaintext = E2eeProtocol.DecryptHistory(content, decryptingKey, message, conversation, type, sender, replyTo, isEdit);
        if (envelope.Sender.UserId != state.Public.UserId ||
            !state.Recovery.Archive.Keys.Any(d => d.Fingerprint == envelope.Sender.Fingerprint))
        {
            var signer = envelope.Sender;
            if (signer.AccountIdentityKey is null)
            {
                var historical = await api.GetAsync<DeviceKey>($"api/e2ee/conversations/{type}/{conversation}/signers/{signer.SessionId}?identityKey={Uri.EscapeDataString(signer.IdentityKey)}");
                if (!historical.IsSuccess || historical.Value is not { AccountIdentityKey: not null } certified ||
                    certified.UserId != signer.UserId || certified.SessionId != signer.SessionId || certified.IdentityKey != signer.IdentityKey)
                    throw new CryptographicException("Исторический ключ ещё не подтверждён устройством автора.");
                signer = certified;
            }
            await TrustAsync(signer, state.Public, state.Scope, envelope.CreatedAt);
        }
        var versionKey = $"{state.Scope}-revision-{message:N}";
        var known = await storage.GetAsync(versionKey);
        if (known is not null)
        {
            var previous = JsonSerializer.Deserialize<AcceptedRevision>(known)!;
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
            if (envelope.CreatedAt < previous.CreatedAt ||
                (envelope.CreatedAt == previous.CreatedAt && digest != previous.Hash))
                throw new CryptographicException("Получена устаревшая или подменённая версия сообщения.");
        }
        foreach (var attachment in plaintext.Attachments)
        {
            if (Convert.FromBase64String(attachment.Key).Length != 32)
                throw new CryptographicException("Неверный ключ вложения.");
            await storage.SetAsync($"{state.Scope}-media-{attachment.MediaId:N}", JsonSerializer.Serialize(attachment));
        }
        await storage.SetAsync(versionKey, JsonSerializer.Serialize(new AcceptedRevision(envelope.CreatedAt,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))))));
        if (jwt.CurrentUserId != state.Public.UserId) throw new UnauthorizedAccessException("Аккаунт изменился.");
        return plaintext.Text;
    }

    public async Task RememberAttachmentAsync(AttachmentSecret secret)
    {
        var state = await ReadyAsync();
        await storage.SetAsync($"{state.Scope}-media-{secret.MediaId:N}", JsonSerializer.Serialize(secret));
    }

    public async Task<AttachmentSecret?> GetAttachmentAsync(Guid id)
    {
        var state = await ReadyAsync();
        return await GetAttachmentAsync(id, state.Scope);
    }

    private async Task<AttachmentSecret?> GetAttachmentAsync(Guid id, string scope)
    {
        var json = await storage.GetAsync($"{scope}-media-{id:N}");
        return json is null ? null : JsonSerializer.Deserialize<AttachmentSecret>(json, E2eeProtocol.Json);
    }

    private sealed record AcceptedRevision(long CreatedAt, string Hash);
    private sealed record SavedDevice(Guid SessionId, DevicePrivateKeys Keys, string Fingerprint,
        long Generation = 0, string? PreviousFingerprint = null, long RotatedAt = 0);
}
