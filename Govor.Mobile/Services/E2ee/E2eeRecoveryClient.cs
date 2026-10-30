using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Govor.Mobile.Services.Api.Base;
using Govor.Security;

namespace Govor.Mobile.Services.E2ee;

public interface IE2eeRecoveryPrompt
{
    Task<string?> RequestCodeAsync();
    Task ShowCreatedAsync(string code);
}

public sealed record AccountArchive(string IdentityPrivateKey, HistoryKey[] Keys,
    Dictionary<Guid, string> TrustedAccounts, Dictionary<Guid, long>? IdentityEpochs = null);
public sealed record LocalRecovery(string Code, long Revision, AccountArchive Archive);

// Called under E2eeClient's account/key lock. Merge before compare-and-swap so
// concurrent devices cannot overwrite each other's history decryption keys.
public sealed class E2eeRecoveryClient(IApiClient api, IE2eeStorage storage, IE2eeRecoveryPrompt? prompt,
    PasswordAccountClient? passwords = null)
{
    public async Task<LocalRecovery> SynchronizeAsync(Guid user, string scope, HistoryKey[] localKeys)
    {
        var encoded = await storage.GetAsync(scope + "-recovery");
        var local = encoded is null ? null : JsonSerializer.Deserialize<LocalRecovery>(encoded, E2eeProtocol.Json)
            ?? throw new CryptographicException("Повреждены локальные данные восстановления.");
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await api.GetAsync<RecoveryState>("api/e2ee/recovery");
            if (!response.IsSuccess || response.Value is null)
                throw new InvalidOperationException("Не удалось загрузить зашифрованный архив E2EE.");
            var remote = response.Value.Vault;
            var created = false;
            if (remote is null)
            {
                if (local is { Revision: > 0 }) throw new CryptographicException("Сервер потерял архив восстановления.");
                if (local is null)
                {
                    local = new(E2eeRecoveryProtocol.GenerateCode(), 0,
                        new(E2eeRecoveryProtocol.GenerateIdentity(), [], []));
                    // The recovery secret must survive a failed first network write.
                    await SaveAsync(scope, local);
                }
                created = true;
            }
            else
            {
                // Two clean installations may race to create the first archive.
                // A provisional local identity has never authorized a message and
                // can be replaced by restoring the winner's archive explicitly.
                if (local is { Revision: 0 } && E2eeRecoveryProtocol.PublicIdentity(local.Archive.IdentityPrivateKey) != remote.IdentityKey)
                    local = null;
                if (local is not null && remote.Revision < local.Revision)
                    throw new CryptographicException("Сервер вернул устаревший архив E2EE.");
                var rootChanged = local is not null && E2eeRecoveryProtocol.PublicIdentity(local.Archive.IdentityPrivateKey) != remote.IdentityKey;
                if (rootChanged && passwords is not null)
                {
                    // A fresh login can open the new password wrapper after an
                    // explicitly authorized reset on another device.
                    local = null;
                    localKeys = localKeys.TakeLast(1).ToArray();
                }
                var code = local?.Code ?? (passwords is null ? null : await passwords.TryRestoreCodeAsync(user))
                    ?? await (prompt?.RequestCodeAsync() ?? Task.FromResult<string?>(null));
                if (string.IsNullOrWhiteSpace(code))
                    throw new CryptographicException("Для нового устройства нужен код с доверенного устройства или резервный код.");
                AccountArchive archive;
                try { archive = JsonSerializer.Deserialize<AccountArchive>(E2eeRecoveryProtocol.Open(remote, user, code), E2eeProtocol.Json)
                    ?? throw new CryptographicException("Неверный архив E2EE."); }
                catch (Exception ex) when (ex is Jose.JoseException or FormatException or JsonException)
                { throw new CryptographicException("Код не подходит или архив повреждён.", ex); }
                if (E2eeRecoveryProtocol.PublicIdentity(archive.IdentityPrivateKey) != remote.IdentityKey ||
                    (local is not null && E2eeRecoveryProtocol.PublicIdentity(local.Archive.IdentityPrivateKey) != remote.IdentityKey))
                    throw new CryptographicException("Ключ аккаунта в архиве подменён.");
                local = new(code.Trim(), remote.Revision, Merge(archive, local?.Archive));
            }
            var merged = Merge(local!.Archive, new(local.Archive.IdentityPrivateKey, localKeys, []));
            // Always retain the merged keys locally, even if the upload is interrupted.
            local = local with { Archive = merged };
            await SaveAsync(scope, local);
            if (remote is not null && JsonSerializer.Serialize(merged) ==
                E2eeRecoveryProtocol.Open(remote, user, local.Code))
            {
                if (passwords is not null) await passwords.EnsureEnvelopeAsync(user, local);
                return local;
            }
            var sealedVault = E2eeRecoveryProtocol.Seal(user, local.Revision + 1,
                merged.IdentityPrivateKey, local.Code, JsonSerializer.Serialize(merged));
            var saved = await api.PutAsync<RecoveryVault>("api/e2ee/recovery", new SaveRecoveryVault(local.Revision,
                sealedVault.IdentityKey, sealedVault.Ciphertext, sealedVault.Signature));
            if (saved.StatusCode == HttpStatusCode.Conflict) continue;
            if (!saved.IsSuccess) throw new InvalidOperationException("Не удалось сохранить архив E2EE. Отправка приостановлена.");
            local = local with { Revision = sealedVault.Revision };
            await SaveAsync(scope, local);
            if (passwords is not null) await passwords.EnsureEnvelopeAsync(user, local);
            if (created && prompt is not null && await storage.GetAsync(scope + "-code-shown") != "yes")
            {
                await prompt.ShowCreatedAsync(local.Code);
                await storage.SetAsync(scope + "-code-shown", "yes");
            }
            return local;
        }
        throw new InvalidOperationException("Архив обновляется на другом устройстве. Повторите попытку.");
    }

    private Task SaveAsync(string scope, LocalRecovery state) =>
        storage.SetAsync(scope + "-recovery", JsonSerializer.Serialize(state));

    private static AccountArchive Merge(AccountArchive first, AccountArchive? second)
    {
        if (first.Keys is null || first.TrustedAccounts is null || first.Keys.Length > 1500 ||
            first.TrustedAccounts.Count > 10_000)
            throw new CryptographicException("Неверный архив ключей.");
        if (second is null) return first;
        var pins = new Dictionary<Guid, string>(first.TrustedAccounts);
        var epochs = new Dictionary<Guid, long>(first.IdentityEpochs ?? []);
        foreach (var (user, identity) in second.TrustedAccounts)
        {
            var firstEpoch = epochs.GetValueOrDefault(user);
            var secondEpoch = second.IdentityEpochs?.GetValueOrDefault(user) ?? 0;
            if (pins.TryGetValue(user, out var known) && known != identity)
            {
                if (firstEpoch == secondEpoch) throw new CryptographicException("Ключ собеседника отличается на разных устройствах.");
                if (firstEpoch > secondEpoch) continue;
            }
            pins[user] = identity;
            epochs[user] = Math.Max(firstEpoch, secondEpoch);
        }
        var keys = first.Keys.Concat(second.Keys).DistinctBy(k => k.Fingerprint).OrderBy(k => k.Fingerprint).ToArray();
        return first with { Keys = keys, TrustedAccounts = pins.OrderBy(p => p.Key).ToDictionary(), IdentityEpochs = epochs.OrderBy(p => p.Key).ToDictionary() };
    }
}
