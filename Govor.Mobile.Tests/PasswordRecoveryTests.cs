using System.Net;
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
public class PasswordRecoveryTests
{
    private const string Password = "password-for-client-tests";
    private static readonly Guid User = Guid.NewGuid(), Session = Guid.NewGuid();
    private PasswordCredentials _credentials = null!;
    private string _key = null!, _root = null!, _code = null!;
    private Api _api = null!; private Storage _storage = null!; private Tokens _tokens = null!;
    private PasswordAccountClient _client = null!;
    [OneTimeSetUp]
    public void Keys()
    {
        (_credentials, _key) = PasswordProtocol.Create(Password);
        _root = E2eeRecoveryProtocol.GenerateIdentity(); _code = E2eeRecoveryProtocol.GenerateCode();
    }
    [SetUp]
    public void Setup()
    {
        _storage = new(); _tokens = new();
        _api = new() { Credentials = _credentials,
            Vault = E2eeRecoveryProtocol.Seal(User, 1, _root, _code, JsonSerializer.Serialize(new AccountArchive(_root, [], []))),
            Envelope = PasswordProtocol.Seal(User, 1, _credentials, _root, _code, _key) };
        _client = new(_api, _tokens, new Server(), _storage);
    }

    [Test]
    public async Task FreshDeviceRestoresArchiveAfterPasswordLoginWithoutRequestingRecoveryCode()
    {
        await _client.LoginAsync("alice", Password, "phone");
        var prompt = new RecoveryPrompt();
        var state = await new E2eeRecoveryClient(_api, _storage, prompt, _client).SynchronizeAsync(User, _client.Scope(User), []);
        Assert.That(state.Code, Is.EqualTo(_code));
        Assert.That(E2eeRecoveryProtocol.PublicIdentity(state.Archive.IdentityPrivateKey), Is.EqualTo(E2eeRecoveryProtocol.PublicIdentity(_root)));
        Assert.That(prompt.Requests, Is.Zero);
        Assert.That(string.Join("\n", _api.Requests), Does.Not.Contain(Password).And.Not.Contain(_key).And.Not.Contain(_root).And.Not.Contain(_code));
    }

    [Test]
    public async Task RegistrationUsesTheSingleRegistrationRouteWithoutSendingPassword()
    {
        _api.Vault = null; _api.Envelope = null;
        await _client.RegisterAsync("alice", Password, "invite-code", "phone");
        Assert.That(_api.Requests.Single(), Does.StartWith("api/auth/register ").And.Not.Contain(Password));
        var prompt = new RecoveryPrompt();
        var first = await new E2eeRecoveryClient(_api, _storage, prompt, _client).SynchronizeAsync(User, _client.Scope(User), []);
        Assert.That(_api.Envelope, Is.Not.Null);
        var freshStorage = new Storage();
        var fresh = new PasswordAccountClient(_api, new Tokens(), new Server(), freshStorage);
        await fresh.LoginAsync("alice", Password, "new phone");
        var restored = await new E2eeRecoveryClient(_api, freshStorage, prompt, fresh).SynchronizeAsync(User, fresh.Scope(User), []);
        Assert.That(restored.Code, Is.EqualTo(first.Code));
        Assert.That(prompt.Requests, Is.Zero);
    }

    [Test]
    public void InvalidServerProofNeverPersistsAKeyForArchiveRecovery()
    {
        _api.CorruptServerProof = true;
        Assert.CatchAsync(() => _client.LoginAsync("alice", Password, "phone"));
        Assert.That(_storage.Values.Keys.Any(k => k.EndsWith("-password-key")), Is.False);
    }

    [Test]
    public async Task KnownSrpAccountNeverAutomaticallySendsPasswordToALegacyEndpoint()
    {
        await _client.LoginAsync("alice", Password, "phone");
        _api.Legacy = true; _api.Requests.Clear();
        Assert.ThrowsAsync<PasswordUpgradeRequiredException>(() => _client.LoginAsync("alice", Password, "phone"));
        Assert.That(_api.Requests, Has.Count.EqualTo(1));
        Assert.That(_api.Requests.Single(), Does.StartWith("api/auth/password/challenge").And.Not.Contain(Password));
    }

    [Test]
    public async Task RejectedPasswordChangeDoesNotAlterTheExistingLocalRecoveryKey()
    {
        await _client.LoginAsync("alice", Password, "phone");
        var savedKey = _storage.Values.Single(p => p.Key.EndsWith("-password-key")).Value;
        var recovery = new LocalRecovery(_code, 1, new(_root, [], []));
        _api.RejectChange = true;
        Assert.ThrowsAsync<InvalidOperationException>(() => _client.ChangeAsync(User, recovery, Password, "new-password-for-client"));
        Assert.That(_storage.Values.Single(p => p.Key.EndsWith("-password-key")).Value, Is.EqualTo(savedKey));
        Assert.That(await _client.TryRestoreCodeAsync(User), Is.EqualTo(_code));
    }

    [Test]
    public async Task InterruptedPasswordChangeCommitsJournaledKeyAfterServerVersionIsConfirmed()
    {
        await _client.LoginAsync("alice", Password, "phone");
        _api.InterruptChangeResponse = true;
        Assert.ThrowsAsync<InvalidOperationException>(() => _client.ChangeAsync(User, new(_code, 1, new(_root, [], [])), Password, "new-password-for-client"));
        Assert.That(await _client.TryRestoreCodeAsync(User), Is.EqualTo(_code));
        Assert.That(_storage.Values[_client.Scope(User) + "-password-change-pending"], Is.EqualTo("null"));
        var fresh = new PasswordAccountClient(_api, new Tokens(), new Server(), new Storage());
        await fresh.LoginAsync("alice", "new-password-for-client", "phone");
        Assert.That(await fresh.TryRestoreCodeAsync(User), Is.EqualTo(_code));
    }

    [Test]
    public async Task InterruptedEncryptionResetRecoversJournalWithoutReusingTheOldSigningKey()
    {
        _client = new(_api, _tokens, new Server(), _storage, new PasswordPrompts());
        await _client.LoginAsync("alice", Password, "phone");
        var e2ee = new E2eeClient(_api, _tokens, new Server(), _storage, new RecoveryPrompt(), passwords: _client);
        await e2ee.InitializeAsync();
        var original = _api.Registered!;
        var localBefore = _storage.Values[_client.Scope(User) + "-recovery"];
        _api.InterruptResetResponse = true;
        Assert.ThrowsAsync<InvalidOperationException>(() => e2ee.ResetEncryptionAsync());
        Assert.That(_storage.Values[_client.Scope(User) + "-recovery"], Is.EqualTo(localBefore));
        Assert.That(_api.Vault!.IdentityKey, Is.Not.EqualTo(original.AccountIdentityKey));
        var restarted = new E2eeClient(_api, _tokens, new Server(), _storage, new RecoveryPrompt(), passwords: _client);
        await restarted.InitializeAsync();
        Assert.That(_api.Registered!.IdentityKey, Is.Not.EqualTo(original.IdentityKey));
        Assert.That(_api.Registered.AccountIdentityKey, Is.EqualTo(_api.Vault!.IdentityKey));
        Assert.That(_storage.Values[_client.Scope(User) + "-reset-operation"], Is.EqualTo("null"));
        var local = JsonSerializer.Deserialize<LocalRecovery>(_storage.Values[_client.Scope(User) + "-recovery"])!;
        Assert.That(local.Archive.Keys, Has.Length.EqualTo(1));
        Assert.That(local.Code, Is.Not.EqualTo(_code));
        Assert.That(await _client.TryRestoreCodeAsync(User), Is.EqualTo(local.Code));
    }

    private sealed class PasswordPrompts : IPasswordAccountPrompts
    {
        public Task<string?> UpgradePasswordAsync() => Task.FromResult<string?>(null);
        public Task<string?> CurrentPasswordAsync(string reason) => Task.FromResult<string?>(Password);
        public Task NotifyIdentityChangedAsync(Guid user) => Task.CompletedTask;
    }

    private sealed class RecoveryPrompt : IE2eeRecoveryPrompt
    {
        public int Requests;
        public Task<string?> RequestCodeAsync() { Requests++; return Task.FromResult<string?>(null); }
        public Task ShowCreatedAsync(string code) => Task.CompletedTask;
    }
    private sealed class Storage : IE2eeStorage
    {
        public Dictionary<string, string> Values { get; } = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
    }
    private sealed class Server : IServerIpProvider { public string IP => "https://govor.example"; }
    private sealed class Tokens : IJwtProviderService
    {
        public bool HasRefreshToken => true;
        public Guid? CurrentUserId => User;
        public event Action? WasClearTokens { add { } remove { } }
        public static string Token => "eyJhbGciOiJub25lIn0." + Jose.Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new { userId = User, sid = Session })) + ".";
        public Task<string> GetAccessTokenAsync() => Task.FromResult(Token);
        public Task<string> RefreshAccessTokenAsync(string? rejectedToken) => GetAccessTokenAsync();
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeWithTokensAsync(string accessToken, string refreshToken) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }
    private sealed class Api : IApiClient
    {
        public PasswordCredentials Credentials = null!;
        public PasswordEnvelope? Envelope;
        public RecoveryVault? Vault;
        public long Version = 1;
        public bool Legacy, CorruptServerProof, RejectChange, InterruptChangeResponse, InterruptResetResponse;
        public DeviceKey? Registered;
        public List<string> Requests { get; } = [];
        private readonly Dictionary<Guid, (string Secret, string Public)> _proofs = [];
        public Task<HttpResult<T>> GetAsync<T>(string endpoint, bool authenticated = true) => Ok<T>(endpoint == "api/auth/password"
            ? new PasswordConfiguration(Credentials, Version, Envelope) : endpoint == "api/e2ee/devices"
            ? new[] { Registered! } : new RecoveryState(Vault));
        public Task<HttpResult<T>> PostAsync<T>(string endpoint, object data, bool authenticated = true)
        {
            Requests.Add(endpoint + " " + JsonSerializer.Serialize(data));
            if (endpoint == "api/auth/register")
            {
                using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(data)); var root = parsed.RootElement;
                Credentials = new(1, root.GetProperty("SrpSalt").GetString()!, root.GetProperty("SrpIdentity").GetString()!, root.GetProperty("SrpVerifier").GetString()!);
                return Ok<T>(new AuthResponse("refresh", Tokens.Token));
            }
            if (data is PasswordChallengeRequest challenge)
            {
                if (Legacy) return Ok<T>(new PasswordChallenge(Guid.Empty, "", "", "", Version, true));
                var ephemeral = PasswordProtocol.Server().GenerateEphemeral(Credentials.Verifier); var id = Guid.NewGuid();
                _proofs[id] = (ephemeral.Secret, challenge.PublicEphemeral);
                return Ok<T>(new PasswordChallenge(id, Credentials.Salt, Credentials.Identity, ephemeral.Public, Version));
            }
            if (data is PasswordProof proof)
            {
                var state = _proofs[proof.ChallengeId]; _proofs.Remove(proof.ChallengeId);
                var session = PasswordProtocol.Server().DeriveSession(state.Secret, state.Public, Credentials.Salt,
                    Credentials.Identity, Credentials.Verifier, proof.Proof);
                var serverProof = CorruptServerProof ? new string('0', 128) : session.Proof;
                return Ok<T>(endpoint.EndsWith("authorize") ? new PasswordActionResult(Guid.NewGuid(), serverProof, Version) :
                    new PasswordLoginResult("refresh", Tokens.Token, serverProof, Version));
            }
            if (data is ChangePasswordRequest change)
            {
                if (RejectChange) return Task.FromResult(new HttpResult<T>(HttpStatusCode.Conflict));
                Credentials = change.Credentials; Envelope = change.Envelope; Version++;
                if (InterruptChangeResponse) return Task.FromResult(new HttpResult<T>(HttpStatusCode.ServiceUnavailable));
                return Ok<T>(default(T)!);
            }
            if (data is ResetEncryptionRequest reset)
            {
                Vault = reset.Vault; Envelope = reset.Envelope;
                if (InterruptResetResponse) return Task.FromResult(new HttpResult<T>(HttpStatusCode.ServiceUnavailable));
                return Ok<T>(new AccountIdentityChange(User, 1, reset.ExpectedIdentityKey, reset.Vault.IdentityKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            }
            throw new NotSupportedException(endpoint);
        }
        public Task<HttpResult<T>> PutAsync<T>(string endpoint, object data, bool authenticated = true)
        {
            Requests.Add(endpoint + " " + JsonSerializer.Serialize(data));
            if (data is SaveRecoveryVault vault) { Vault = new(User, vault.ExpectedRevision + 1, vault.IdentityKey, vault.Ciphertext, vault.Signature); return Ok<T>(Vault); }
            if (data is SavePasswordEnvelope envelope) { Envelope = envelope.Envelope; return Ok<T>(default(T)!); }
            if (data is DeviceKey device) { Registered = device; return Ok<T>(default(T)!); }
            throw new NotSupportedException(endpoint);
        }
        private static Task<HttpResult<T>> Ok<T>(object value) => Task.FromResult(HttpResult<T>.Success((T)value));
        public Task<HttpResult<bool>> DeleteAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<UploadMediaResponse>> PostMultipartAsync(string endpoint, MultipartFormDataContent form, bool authenticated = true) => throw new NotSupportedException();
        public Task<HttpResult<Govor.Mobile.Utilities.FileResult>> GetFileStreamAsync(string endpoint, bool authenticated = true) => throw new NotSupportedException();
    }
}
