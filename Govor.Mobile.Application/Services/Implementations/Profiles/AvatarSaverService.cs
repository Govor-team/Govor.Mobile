using Govor.Mobile.Application.Services.Interfaces.Profiles;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Govor.Mobile.Application.Services.Implementations.Profiles;

public class AvatarSaverService : IAvatarSaver
{
    private readonly IAvatarStoragePath _storage;
    private readonly ILogger<AvatarSaverService>? _logger;

    public AvatarSaverService(IAvatarStoragePath storage, ILogger<AvatarSaverService>? logger = null)
    {
        _storage = storage;
        _logger = logger;

        // Гарантируем, что папка существует
        Directory.CreateDirectory(_storage.AvatarsFolder);
    }

    public void DeleteAvatarAsync(string fileName)
    {
        var filePath = Path.Combine(_storage.AvatarsFolder, fileName);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger?.LogInformation("Avatar deleted: {FilePath}", filePath);
            }
            catch (IOException ex)
            {
                _logger?.LogWarning(ex, "Avatar locked, will retry later: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete avatar: {FilePath}", filePath);
            }
        }
    }

    public async Task<string> SaveAvatarAsync(Guid id, string fileName, Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Stream is not readable", nameof(stream));

        var filePath = Path.Combine(_storage.AvatarsFolder, $"{id}.{Path.GetExtension(fileName)}");

        // Перематываем поток на начало, если нужно
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        return filePath;
    }
}
