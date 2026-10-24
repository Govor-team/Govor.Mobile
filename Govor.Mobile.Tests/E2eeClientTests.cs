using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.E2ee;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Utilities;
using Govor.Security;

namespace Govor.Mobile.Tests;

[TestFixture]
public class E2eeClientTests
{
    private static readonly Guid Alice = Guid.NewGuid(), Bob = Guid.NewGuid(), Chat = Guid.NewGuid();
    private DevicePrivateKeys _bobKeys = null!;
    private string _bobIdentity = null!;
    private DeviceKey _bob = null!;
    private Storage _storage = null!;
    private Prompt _prompt = null!;
    private Api _api = null!;
    private Tokens _tokens = null!;
    private E2eeClient _client = null!;

    [OneTimeSetUp] public void Generate()
    { _bobKeys = E2eeProtocol.GenerateKeys(); _bobIdentity = E2eeRecoveryProtocol.GenerateIdentity(); }
    [SetUp]
    public void Setup()
    {
        _bob = E2eeRecoveryProtocol.Certify(E2eeProtocol.PublicKey(_bobKeys, Bob, Guid.NewGuid()), _bobIdentity);
        _storage = new(); _prompt = new(); _tokens = new(); _api = new() { Bob = _bob };
        _client = new(_api, _tokens, new Server(), _storage, _prompt);
    }

    [Test]
    public async Task SendsOnlyCiphertextAndRegistersOnlyPublicKeys()
    {
        var encrypted = await _client.EncryptAsync("secret text", Chat, 0, null, []);
        var id = E2eeProtocol.Inspect(encrypted).MessageId;
        Assert.That(E2eeProtocol.Decrypt(encrypted, _bobKeys, id, Chat, 0, Alice).Text, Is.EqualTo("secret text"));
        Assert.That(JsonSerializer.Serialize(_api.Registered), Does.Not.Contain("Private"));
        Assert.That(encrypted, Does.Not.Contain("secret text"));
        Assert.That(_prompt.Count, Is.Zero);
    }

    [Test]
    public async Task CertifiedNewDevicesDoNotRequireDialogs()
    {
        await _client.EncryptAsync("first", Chat, 0, null, []);
        await _client.EncryptAsync("second", Chat, 0, null, []);
        Assert.That(_prompt.Count, Is.Zero);
        var replacement = E2eeProtocol.GenerateKeys();
        _api.Bob = E2eeRecoveryProtocol.Certify(E2eeProtocol.PublicKey(replacement, Bob, Guid.NewGuid()), _bobIdentity);
        await _client.EncryptAsync("third", Chat, 0, null, []);
        Assert.That(_prompt.Count, Is.Zero);
    }

    [Test]
    public async Task ReplacingPinnedAccountIdentityStopsSending()
    {
        await _client.EncryptAsync("first", Chat, 0, null, []);
        _api.Bob = E2eeRecoveryProtocol.Certify(_bob, E2eeRecoveryProtocol.GenerateIdentity());
        Assert.ThrowsAsync<CryptographicException>(() => _client.EncryptAsync("never send", Chat, 0, null, []));
    }

    [Test]
    public async Task FirstConversationNeedsNoKeyOrRosterConfirmation()
    {
        await _client.EncryptAsync("approved", Chat, 0, null, []);
        Assert.That(_prompt.Count, Is.Zero);
    }

    [Test]
    public void KeyPersistenceFailureStopsRegistrationAndSending()
    {
        _storage.FailWrites = true;
        Assert.ThrowsAsync<IOException>(() => _client.EncryptAsync("never send", Chat, 0, null, []));
        Assert.That(_api.Registered, Is.Null);
    }

    [Test]
    public async Task RegistrationFailureNeverFallsBackToPlaintext()
    {
        _api.RegisterOk = false;
        Assert.ThrowsAsync<InvalidOperationException>(() => _client.EncryptAsync("never send", Chat, 0, null, []));
        await Task.CompletedTask;
    }

    [Test]
    public async Task LoginUsesNewKeysWhileDeviceRetainsHistoricalDecryptionKeys()
    {
        var old = await _client.EncryptAsync("history", Chat, 0, null, []);
        var firstFingerprint = _api.Registered!.Fingerprint;
        _tokens.Session = Guid.NewGuid();
        await _client.InitializeAsync();
        Assert.That(_api.Registered!.Fingerprint, Is.Not.EqualTo(firstFingerprint));
        var e = E2eeProtocol.Inspect(old);
        Assert.That(await _client.DecryptAsync(old, e.MessageId, Chat, 0, Alice, null), Is.EqualTo("history"));
    }

    [Test]
    public async Task RejectsReplayedOlderRevisionAfterReceivingAnEdit()
    {
        await _client.InitializeAsync();
        var directory = new ConversationKeys(Chat, 0, [Alice, Bob], [_api.Registered!, _bob]);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var first = E2eeProtocol.Encrypt(new("first", []), _bobKeys, _bob, directory, id, createdAt: now);
        var edit = E2eeProtocol.Encrypt(new("edit", []), _bobKeys, _bob, directory, id, isEdit: true, createdAt: now + 1);
        Assert.That(await _client.DecryptAsync(first, id, Chat, 0, Bob, null), Is.EqualTo("first"));
        Assert.That(await _client.DecryptAsync(edit, id, Chat, 0, Bob, null), Is.EqualTo("edit"));
        Assert.ThrowsAsync<CryptographicException>(() => _client.DecryptAsync(first, id, Chat, 0, Bob, null));
    }

    [Test]
    public async Task IncomingAttachmentKeyIsKeptOnlyOnTheDevice()
    {
        await _client.InitializeAsync();
        var directory = new ConversationKeys(Chat, 0, [Alice, Bob], [_api.Registered!, _bob]);
        var (bytes, key) = E2eeProtocol.EncryptAttachment("private bytes"u8.ToArray());
        var secret = new AttachmentSecret(Guid.NewGuid(), key, "private.txt", "text/plain");
        var id = Guid.NewGuid();
        var envelope = E2eeProtocol.Encrypt(new("file", [secret]), _bobKeys, _bob, directory, id);
        await _client.DecryptAsync(envelope, id, Chat, 0, Bob, null);
        Assert.That(await _client.GetAttachmentAsync(secret.MediaId), Is.EqualTo(secret));
        Assert.That(System.Text.Encoding.UTF8.GetString(E2eeProtocol.DecryptAttachment(bytes, secret.Key)), Is.EqualTo("private bytes"));
    }

    [Test]
    public async Task FreshPhysicalDeviceRestoresHistoryWithoutAnotherDevicesSigningKey()
    {
        var old = await _client.EncryptAsync("history", Chat, 0, null, []);
        var code = await _client.GetRecoveryCodeAsync();
        var saved = JsonDocument.Parse(_storage.Values.Single(p => p.Key.Contains("-key-")).Value);
        var signingKey = saved.RootElement.GetProperty("Keys").GetProperty("IdentityKey").GetString()!;
        Assert.That(E2eeRecoveryProtocol.Open(_api.Vault!, Alice, code), Does.Not.Contain(signingKey));
        var fresh = new E2eeClient(_api, new Tokens(), new Server(), new Storage(), new Prompt { Code = code });
        var e = E2eeProtocol.Inspect(old);
        Assert.That(await fresh.DecryptAsync(old, e.MessageId, Chat, 0, Alice, null), Is.EqualTo("history"));
        Assert.That(_api.Registered!.SessionId, Is.Not.EqualTo(_tokens.Session));
        Assert.That(_api.Registered.AccountIdentityKey, Is.EqualTo(e.Sender.AccountIdentityKey));
    }

    [Test]
    public async Task WrongRecoveryCodeDoesNotRegisterNewDevice()
    {
        await _client.InitializeAsync();
        var originalSession = _api.Registered!.SessionId;
        var fresh = new E2eeClient(_api, new Tokens(), new Server(), new Storage(),
            new Prompt { Code = E2eeRecoveryProtocol.GenerateCode() });
        Assert.ThrowsAsync<CryptographicException>(() => fresh.InitializeAsync());
        Assert.That(_api.Registered.SessionId, Is.EqualTo(originalSession));
    }

    [Test]
    public async Task SevenDayRotationExcludesOldKeyAndRetainsHistoryOnFreshDevice()
    {
        var clock = new TestClock();
        _client = new(_api, _tokens, new Server(), _storage, _prompt, clock);
        var first = await _client.EncryptAsync("before", Chat, 0, null, []);
        var oldFingerprint = _api.Registered!.Fingerprint;
        var historyKey = JsonSerializer.Deserialize<LocalRecovery>(_storage.Values.Single(p => p.Key.EndsWith("-recovery")).Value)!
            .Archive.Keys.Single(k => k.Fingerprint == oldFingerprint);
        clock.Now = clock.Now.AddDays(8);
        var second = await _client.EncryptAsync("after", Chat, 0, null, []);
        Assert.That(_api.Registered!.Generation, Is.EqualTo(1));
        Assert.That(_api.Registered.PreviousFingerprint, Is.EqualTo(oldFingerprint));
        Assert.That(E2eeProtocol.Inspect(second).Recipients, Does.Not.Contain(oldFingerprint));
        Assert.Throws<CryptographicException>(() => E2eeProtocol.DecryptHistory(second, historyKey,
            E2eeProtocol.Inspect(second).MessageId, Chat, 0, Alice));
        var fresh = new E2eeClient(_api, new Tokens(), new Server(), new Storage(),
            new Prompt { Code = await _client.GetRecoveryCodeAsync() }, clock);
        foreach (var content in new[] { first, second })
        {
            var e = E2eeProtocol.Inspect(content);
            Assert.That(await fresh.DecryptAsync(content, e.MessageId, Chat, 0, Alice, null),
                Is.EqualTo(content == first ? "before" : "after"));
        }
    }

    [Test]
    public async Task InterruptedRotationRetriesTheSameJournaledKey()
    {
        var clock = new TestClock();
        _client = new(_api, _tokens, new Server(), _storage, _prompt, clock);
        await _client.InitializeAsync();
        clock.Now = clock.Now.AddDays(8);
        _api.RegisterOk = false;
        Assert.ThrowsAsync<InvalidOperationException>(() => _client.InitializeAsync());
        var pending = JsonDocument.Parse(_storage.Values.Single(p => p.Key.Contains("-pending-")).Value);
        _api.RegisterOk = true;
        await _client.InitializeAsync();
        Assert.That(_api.Registered!.Fingerprint, Is.EqualTo(pending.RootElement.GetProperty("Fingerprint").GetString()));
    }

    [Test]
    public async Task ArchiveCompareAndSwapConflictPreservesBothDevicesHistoryKeys()
    {
        await _client.InitializeAsync();
        var code = await _client.GetRecoveryCodeAsync();
        var first = _api.Registered!.Fingerprint;
        var archive = JsonSerializer.Deserialize<AccountArchive>(E2eeRecoveryProtocol.Open(_api.Vault!, Alice, code))!;
        var concurrentKeys = E2eeProtocol.GenerateKeys();
        var concurrentKey = new HistoryKey(E2eeProtocol.PublicKey(concurrentKeys, Alice, Guid.NewGuid()).IdentityKey, concurrentKeys.EncryptionKey);
        _api.OnConflictVault = E2eeRecoveryProtocol.Seal(Alice, _api.Vault!.Revision + 1, archive.IdentityPrivateKey, code,
            JsonSerializer.Serialize(archive with { Keys = archive.Keys.Append(concurrentKey).ToArray() }));
        _api.ConflictNextVaultWrite = true;
        var second = new E2eeClient(_api, new Tokens(), new Server(), new Storage(), new Prompt { Code = code });
        await second.InitializeAsync();
        var keys = JsonSerializer.Deserialize<AccountArchive>(E2eeRecoveryProtocol.Open(_api.Vault!, Alice, code))!.Keys;
        Assert.That(keys.Select(k => k.Fingerprint), Does.Contain(first).And.Contain(_api.Registered!.Fingerprint));
        Assert.That(keys.Select(k => k.Fingerprint), Does.Contain(concurrentKey.Fingerprint));
        Assert.That(_api.Conflicts, Is.EqualTo(1));
    }

    [Test]
    public async Task ArchiveRollbackAndSignatureTamperingAreRejected()
    {
        await _client.InitializeAsync();
        var previous = _api.Vault!;
        _tokens.Session = Guid.NewGuid();
        await _client.InitializeAsync();
        var current = _api.Vault!;
        _api.Vault = previous;
        Assert.ThrowsAsync<CryptographicException>(() => _client.GetRecoveryCodeAsync());
        _api.Vault = current with { Ciphertext = current.Ciphertext + "x" };
        Assert.ThrowsAsync<CryptographicException>(() => _client.GetRecoveryCodeAsync());
    }

    [Test]
    public async Task LegacySignaturesCanBeAuthenticatedThroughTheAuthorsAccountCertificate()
    {
        await _client.InitializeAsync();
        var legacy = E2eeProtocol.PublicKey(_bobKeys, Bob, _bob.SessionId);
        var directory = new ConversationKeys(Chat, 0, [Alice, Bob], [_api.Registered!, _bob]);
        var id = Guid.NewGuid();
        var content = E2eeProtocol.Encrypt(new("v1 history", []), _bobKeys, legacy, directory, id);
        Assert.That(await _client.DecryptAsync(content, id, Chat, 0, Bob, null), Is.EqualTo("v1 history"));
    }

    [Test]
    public async Task ServerSuppliedLegacyDevicesAreNeverCertifiedWithoutALocalDecryptionKey()
    {
        _api.HistoricalDevices = [E2eeProtocol.PublicKey(E2eeProtocol.GenerateKeys(), Alice, Guid.NewGuid())];
        await _client.InitializeAsync();
        Assert.That(_api.CertificateWrites, Is.Zero);
    }

    [Test]
    public async Task LegacyPeerCannotPinAnUncertifiedKeyAndBlockItsLaterAccountUpgrade()
    {
        _api.Bob = E2eeProtocol.PublicKey(_bobKeys, Bob, _bob.SessionId);
        Assert.ThrowsAsync<CryptographicException>(() => _client.EncryptAsync("update required", Chat, 0, null, []));
        _api.Bob = _bob;
        await _client.EncryptAsync("upgraded", Chat, 0, null, []);
    }

    private sealed class TestClock : TimeProvider
    { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }

    private class Storage : IE2eeStorage
    {
        public Dictionary<string, string> Values { get; } = [];
        public bool FailWrites;
        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value)
        { if (FailWrites) throw new IOException(); Values[key] = value; return Task.CompletedTask; }
    }
    private class Prompt : IE2eeRecoveryPrompt
    {
        public string? Code;
        public int Count;
        public Task<string?> RequestCodeAsync() { Count++; return Task.FromResult(Code); }
        public Task ShowCreatedAsync(string code) { Code = code; return Task.CompletedTask; }
    }
    private class Server : IServerIpProvider { public string IP => "https://govor.example"; }
    private class Tokens : IJwtProviderService
    {
        public Guid Session = Guid.NewGuid();
        public bool HasRefreshToken => true;
        public Guid? CurrentUserId => Alice;
        public event Action? WasClearTokens { add { } remove { } }
        public Task<string> GetAccessTokenAsync() => Task.FromResult("eyJhbGciOiJub25lIn0." +
            Jose.Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { userId = Alice, sid = Session })) + ".");
        public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetAccessTokenAsync();
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }
    private class Api : IApiClient
    {
        public DeviceKey? Registered;
        public DeviceKey Bob = null!;
        public bool RegisterOk = true;
        public RecoveryVault? Vault;
        public RecoveryVault? OnConflictVault;
        public DeviceKey[]? HistoricalDevices;
        public int CertificateWrites;
        public bool ConflictNextVaultWrite;
        public int Conflicts;
        public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true) =>
            Task.FromResult(HttpResult<T>.Success((T)(object)(endpoint.Contains("/signers/") ? Bob :
                endpoint == "api/e2ee/devices" ? HistoricalDevices ?? new[] { Registered! } :
                endpoint == "api/e2ee/recovery" ? new RecoveryState(Vault) :
                new ConversationKeys(Chat, 0, [Alice, E2eeClientTests.Bob], [Registered!, Bob]))));
        public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true)
        {
            if (data is SaveRecoveryVault request)
            {
                if (ConflictNextVaultWrite)
                {
                    ConflictNextVaultWrite = false; Conflicts++; Vault = OnConflictVault ?? Vault;
                    return Task.FromResult(new HttpResult<T>(HttpStatusCode.Conflict));
                }
                if (request.ExpectedRevision != (Vault?.Revision ?? 0))
                    return Task.FromResult(new HttpResult<T>(HttpStatusCode.Conflict));
                Vault = new(Alice, request.ExpectedRevision + 1, request.IdentityKey, request.Ciphertext, request.Signature);
                E2eeRecoveryProtocol.Validate(Vault);
                return Task.FromResult(HttpResult<T>.Success((T)(object)Vault));
            }
            if (!RegisterOk) return Task.FromResult(new HttpResult<T>(HttpStatusCode.Conflict));
            if (endpoint == "api/e2ee/devices/certificate")
            { CertificateWrites++; return Task.FromResult(HttpResult<T>.Success(default!)); }
            Registered = (DeviceKey)data;
            return Task.FromResult(HttpResult<T>.Success(default!));
        }
        public Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<Govor.Mobile.Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
    }
}
