using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Security;

namespace Govor.Mobile.Services.E2ee;

public interface IPasswordAccountPrompts
{
    Task<string?> UpgradePasswordAsync();
    Task<string?> CurrentPasswordAsync(string reason);
    Task NotifyIdentityChangedAsync(Guid user);
}
public sealed record SavedPasswordKey(string Key, string Salt, string Identity, long CredentialVersion);
public sealed class PasswordUpgradeRequiredException() : InvalidOperationException(
    "Способ входа в аккаунт был изменён или пароль сброшен администратором. " +
    "Если это ожидаемый сброс, выберите «Продолжить после сброса пароля». Иначе проверьте адрес сервера.");

public sealed class PasswordAccountClient(IApiClient api, IJwtProviderService jwt, IServerIpProvider server,
    IE2eeStorage storage, IPasswordAccountPrompts? prompts = null)
{
    public string Scope(Guid user) => "govor-e2ee-v1-" +
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new Uri(server.IP).GetLeftPart(UriPartial.Authority)))) + "-" + user.ToString("N");
    private string LoginPin(string name) => "govor-password-mode-" + Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(new Uri(server.IP).GetLeftPart(UriPartial.Authority) + "\n" + name)));

    public async Task<AuthResponse> LoginAsync(string name, string password, string deviceInfo)
    {
        var client = PasswordProtocol.Client();
        var ephemeral = client.GenerateEphemeral();
        var response = await api.PostAsync<PasswordChallenge>("api/auth/password/challenge",
            new PasswordChallengeRequest(name, ephemeral.Public, deviceInfo), false);
        var challenge = Require(response, "Не удалось начать защищённый вход.");
        if (challenge.Legacy)
        {
            if (await storage.GetAsync(LoginPin(name)) == "srp")
                throw new PasswordUpgradeRequiredException();
            return await UpgradeLegacyAsync(name, password, deviceInfo);
        }
        if (challenge.ChallengeId == Guid.Empty || !PasswordProtocol.Hex(challenge.PublicEphemeral, 1024))
            throw new CryptographicException("Неверные параметры защищённого входа.");
        var (secret, key) = await Task.Run(() => PasswordProtocol.Derive(password, challenge.Salt, challenge.Identity));
        var session = await Task.Run(() => client.DeriveSession(ephemeral.Secret, challenge.PublicEphemeral,
            challenge.Salt, challenge.Identity, client.DerivePrivateKey(challenge.Salt, challenge.Identity, secret)));
        var loggedIn = Require(await api.PostAsync<PasswordLoginResult>("api/auth/password/login",
            new PasswordProof(challenge.ChallengeId, session.Proof), false), "Неверный пароль или вход не подтверждён.");
        client.VerifySession(ephemeral.Public, session, loggedIn.ServerProof);
        if (loggedIn.CredentialVersion != challenge.CredentialVersion) throw new CryptographicException("Пароль изменился во время входа.");
        var user = UserId(loggedIn.AccessToken);
        await storage.SetAsync(Scope(user) + "-password-key", JsonSerializer.Serialize(new SavedPasswordKey(key,
            challenge.Salt, challenge.Identity, challenge.CredentialVersion)));
        await storage.SetAsync(LoginPin(name), "srp");
        return new(loggedIn.RefreshToken, loggedIn.AccessToken);
    }

    public async Task<AuthResponse> RegisterAsync(string name, string password, string invitation, string deviceInfo)
    {
        var created = await Task.Run(() => PasswordProtocol.Create(password));
        var response = Require(await api.PostAsync<AuthResponse>("api/auth/register", new
        {
            Name = name, InviteLink = invitation, DeviceInfo = deviceInfo,
            SrpSalt = created.Credentials.Salt, SrpIdentity = created.Credentials.Identity, SrpVerifier = created.Credentials.Verifier
        }, false), "Не удалось зарегистрироваться.");
        var user = UserId(response.accessToken);
        await storage.SetAsync(Scope(user) + "-password-key", JsonSerializer.Serialize(new SavedPasswordKey(created.ArchiveKey,
            created.Credentials.Salt, created.Credentials.Identity, 1)));
        await storage.SetAsync(LoginPin(name), "srp");
        return response;
    }

    // Only this explicit, one-time transition sends the old legacy password.
    // The replacement password and archive key never leave the client.
    private async Task<AuthResponse> UpgradeLegacyAsync(string name, string oldPassword, string deviceInfo)
    {
        var replacement = await (prompts?.UpgradePasswordAsync() ?? Task.FromResult<string?>(null));
        if (string.IsNullOrWhiteSpace(replacement)) throw new InvalidOperationException("Для старого аккаунта нужно один раз обновить пароль.");
        if (replacement == oldPassword) throw new InvalidOperationException("Выберите новый пароль, отличный от прежнего.");
        var credentials = await Task.Run(() => PasswordProtocol.Create(replacement));
        var legacy = Require(await api.PostAsync<AuthResponse>("api/auth/login", new LoingRequest(name, oldPassword, deviceInfo), false), "Неверный прежний пароль.");
        await jwt.InitializeWithTokensAsync(legacy.accessToken, legacy.refreshToken);
        var enrolled = await api.PutAsync<JsonElement>("api/auth/password/enrollment", new PasswordEnrollmentRequest(credentials.Credentials, oldPassword));
        if (!enrolled.IsSuccess) { await jwt.ClearAsync(); throw new InvalidOperationException("Не удалось обновить способ входа."); }
        await api.DeleteAsync("api/session/close");
        await jwt.ClearAsync();
        return await LoginAsync(name, replacement, deviceInfo);
    }

    public Task<AuthResponse> UpgradeAfterPasswordResetAsync(string name, string resetPassword, string deviceInfo) =>
        UpgradeLegacyAsync(name, resetPassword, deviceInfo);

    public async Task<string?> TryRestoreCodeAsync(Guid user)
    {
        var key = await LoadKeyAsync(user);
        if (key is null) return null;
        var configuration = Require(await api.GetAsync<PasswordConfiguration>("api/auth/password"), "Не удалось проверить парольный архив.");
        CheckKey(key, configuration);
        return configuration.Envelope is null ? null : PasswordProtocol.Open(user, configuration.Credentials, configuration.Envelope, key.Key);
    }

    public async Task EnsureEnvelopeAsync(Guid user, LocalRecovery recovery)
    {
        var key = await LoadKeyAsync(user);
        if (key is null) return; // Existing refresh-token sessions enroll after a password login.
        var configuration = Require(await api.GetAsync<PasswordConfiguration>("api/auth/password"), "Не удалось проверить парольный архив.");
        CheckKey(key, configuration);
        if (configuration.Envelope is not null)
        {
            if (configuration.Envelope.IdentityKey != E2eeRecoveryProtocol.PublicIdentity(recovery.Archive.IdentityPrivateKey) ||
                PasswordProtocol.Open(user, configuration.Credentials, configuration.Envelope, key.Key) != recovery.Code)
                throw new CryptographicException("Парольная копия ключа восстановления не совпадает с архивом.");
            return;
        }
        var envelope = PasswordProtocol.Seal(user, configuration.CredentialVersion, configuration.Credentials,
            recovery.Archive.IdentityPrivateKey, recovery.Code, key.Key);
        if (!(await api.PutAsync<JsonElement>("api/auth/password/recovery", new SavePasswordEnvelope(envelope))).IsSuccess)
            throw new InvalidOperationException("Не удалось сохранить восстановление истории по паролю.");
    }

    public async Task<(PasswordActionResult Grant, SavedPasswordKey Key)> AuthorizeAsync(Guid user, string action, string? password = null)
    {
        password ??= await (prompts?.CurrentPasswordAsync(action == "change" ? "Подтвердите смену пароля" : "Подтвердите начало общения с новыми ключами")
            ?? Task.FromResult<string?>(null));
        if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Действие отменено.");
        var client = PasswordProtocol.Client(); var ephemeral = client.GenerateEphemeral();
        var challenge = Require(await api.PostAsync<PasswordChallenge>($"api/auth/password/{action}/challenge",
            new PasswordChallengeRequest("", ephemeral.Public)), "Не удалось подтвердить пароль.");
        if (challenge.Legacy) throw new InvalidOperationException("Сначала выполните вход с обновлением пароля.");
        var derived = await Task.Run(() => PasswordProtocol.Derive(password, challenge.Salt, challenge.Identity));
        var session = await Task.Run(() => client.DeriveSession(ephemeral.Secret, challenge.PublicEphemeral,
            challenge.Salt, challenge.Identity, client.DerivePrivateKey(challenge.Salt, challenge.Identity, derived.Secret)));
        var grant = Require(await api.PostAsync<PasswordActionResult>($"api/auth/password/{action}/authorize",
            new PasswordProof(challenge.ChallengeId, session.Proof)), "Неверный пароль.");
        client.VerifySession(ephemeral.Public, session, grant.ServerProof);
        if (grant.CredentialVersion != challenge.CredentialVersion) throw new CryptographicException("Пароль изменился.");
        var key = new SavedPasswordKey(derived.ArchiveKey, challenge.Salt, challenge.Identity, grant.CredentialVersion);
        return (grant, key);
    }

    public async Task ChangeAsync(Guid user, LocalRecovery recovery, string oldPassword, string newPassword)
    {
        var authorized = await AuthorizeAsync(user, "change", oldPassword);
        var credentials = await Task.Run(() => PasswordProtocol.Create(newPassword));
        var envelope = PasswordProtocol.Seal(user, authorized.Grant.CredentialVersion + 1, credentials.Credentials,
            recovery.Archive.IdentityPrivateKey, recovery.Code, credentials.ArchiveKey);
        var savedKey = new SavedPasswordKey(credentials.ArchiveKey, credentials.Credentials.Salt,
            credentials.Credentials.Identity, authorized.Grant.CredentialVersion + 1);
        await storage.SetAsync(Scope(user) + "-password-change-pending", JsonSerializer.Serialize(savedKey));
        if (!(await api.PostAsync<JsonElement>("api/auth/password/change", new ChangePasswordRequest(
                authorized.Grant.AuthorizationId, credentials.Credentials, envelope))).IsSuccess)
            throw new InvalidOperationException("Не удалось сменить пароль; история не изменена.");
        await storage.SetAsync(Scope(user) + "-password-key", JsonSerializer.Serialize(savedKey));
        await storage.SetAsync(Scope(user) + "-password-change-pending", "null");
    }

    public async Task ResetAsync(Guid user, RecoveryVault previous, LocalRecovery replacement)
    {
        var authorized = await AuthorizeAsync(user, "reset");
        var configuration = Require(await api.GetAsync<PasswordConfiguration>("api/auth/password"), "Не удалось загрузить параметры аккаунта.");
        CheckKey(authorized.Key, configuration);
        var vault = E2eeRecoveryProtocol.Seal(user, previous.Revision + 1, replacement.Archive.IdentityPrivateKey,
            replacement.Code, JsonSerializer.Serialize(replacement.Archive));
        var envelope = PasswordProtocol.Seal(user, configuration.CredentialVersion, configuration.Credentials,
            replacement.Archive.IdentityPrivateKey, replacement.Code, authorized.Key.Key);
        var result = await api.PostAsync<AccountIdentityChange>("api/auth/password/reset-encryption", new ResetEncryptionRequest(
            authorized.Grant.AuthorizationId, previous.IdentityKey, previous.Revision, vault, envelope));
        if (!result.IsSuccess) throw new InvalidOperationException("Не удалось сбросить шифрование. Повторите попытку.");
        await storage.SetAsync(Scope(user) + "-password-key", JsonSerializer.Serialize(authorized.Key));
    }

    public Task NotifyIdentityChangedAsync(Guid user) => prompts?.NotifyIdentityChangedAsync(user) ?? Task.CompletedTask;
    private async Task<SavedPasswordKey?> LoadKeyAsync(Guid user)
    {
        var pending = await storage.GetAsync(Scope(user) + "-password-change-pending");
        if (pending is not null && pending != "null")
        {
            var candidate = JsonSerializer.Deserialize<SavedPasswordKey>(pending, E2eeProtocol.Json)!;
            var config = Require(await api.GetAsync<PasswordConfiguration>("api/auth/password"), "Не удалось проверить завершение смены пароля.");
            if (candidate.CredentialVersion == config.CredentialVersion && candidate.Identity == config.Credentials.Identity &&
                candidate.Salt == config.Credentials.Salt)
            {
                await storage.SetAsync(Scope(user) + "-password-key", pending);
                await storage.SetAsync(Scope(user) + "-password-change-pending", "null");
            }
        }
        var json = await storage.GetAsync(Scope(user) + "-password-key");
        return json is null ? null : JsonSerializer.Deserialize<SavedPasswordKey>(json, E2eeProtocol.Json);
    }
    private static void CheckKey(SavedPasswordKey key, PasswordConfiguration configuration)
    {
        if (key.CredentialVersion != configuration.CredentialVersion || key.Identity != configuration.Credentials.Identity ||
            key.Salt != configuration.Credentials.Salt) throw new CryptographicException("Пароль аккаунта изменился. Войдите заново.");
    }
    private static Guid UserId(string token) => Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(c => c.Type == "userId").Value);
    private static T Require<T>(Govor.Mobile.Utilities.HttpResult<T> result, string message) => result.IsSuccess && result.Value is not null
        ? result.Value : throw new InvalidOperationException(message);
}
