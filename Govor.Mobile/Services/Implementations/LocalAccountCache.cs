using System.Text.Json;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

public sealed class LocalAccountCache(IJwtProviderService session, ILogger<LocalAccountCache> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? GetPath(string key) => session.CurrentUserId is Guid id
        ? Path.Combine(FileSystem.AppDataDirectory, $"{id:N}-{key}.json") : null;

    public async Task<T?> ReadAsync<T>(string key)
    {
        var path = GetPath(key);
        if (path is null) return default;
        await _gate.WaitAsync();
        try
        {
            if (!File.Exists(path)) return default;
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path));
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogWarning(ex, "Unable to read local cache {Key}.", key);
            return default;
        }
        finally { _gate.Release(); }
    }

    public async Task WriteAsync<T>(string key, T value)
    {
        var path = GetPath(key);
        if (path is null) return;
        await _gate.WaitAsync();
        try
        {
            var temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value));
            File.Move(temporary, path, overwrite: true);
        }
        catch (IOException ex) { logger.LogWarning(ex, "Unable to write local cache {Key}.", key); }
        finally { _gate.Release(); }
    }
}
