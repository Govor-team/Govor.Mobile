using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class UserAvatarFileService : IUserAvatarFileService
{
    private readonly IAvatarStoragePath _storage;
    private readonly ILogger<UserAvatarFileService>? _logger;

    public UserAvatarFileService(IAvatarStoragePath storage, ILogger<UserAvatarFileService>? logger = null)
    {
        _storage = storage;
        _logger = logger;

        // Гарантируем, что папка существует
        Directory.CreateDirectory(_storage.UserAvatarsFolder);
    }

    public async Task DeleteLocalAvatarAsync()
    {
        var searchPattern = $"*.*";
        var files = Directory.GetFiles(_storage.UserAvatarsFolder, searchPattern);

        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
                _logger?.LogInformation("Avatar deleted: {FilePath}", file);
            }
            catch (IOException ex)
            {
                _logger?.LogWarning(ex, "Avatar locked, will retry later: {FilePath}", file);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete avatar: {FilePath}", file);
            }
        }
    }

    public async Task<string> SaveAvatarAsync(Guid id, string fileName, Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException("Stream is not readable", nameof(stream));
        
        await DeleteLocalAvatarAsync();
        
        var fileExtension = Path.GetExtension(fileName);
        
        var filePath = _storage.GetAvatarFilePath(id, fileExtension); 
        
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);

        return filePath;
    }
    
    public Task<ImageSource?> LoadLocalAvatarAsync(Guid id)
    {
        var searchPattern = $"{id}.*";
        var files = Directory.GetFiles(_storage.UserAvatarsFolder, searchPattern);
        
        var filePath = files.FirstOrDefault();

        if (string.IsNullOrEmpty(filePath))
        {
            _logger?.LogDebug("Local avatar not found for ID: {AvatarId}", id);
            return Task.FromResult<ImageSource?>(null);
        }

        _logger?.LogInformation("Local avatar loaded from: {FilePath}", filePath);
        
        return Task.FromResult<ImageSource?>(ImageSource.FromFile(filePath));
    }
}
