using System.Collections.Concurrent;
using Govor.Mobile.Models.Requests;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class CurrentUserAvatarService : ICurrentUserAvatarService
{
    private readonly IProfileHubService _profileHub;
    private readonly IMediaLoaderService _mediaLoaderService;
    private readonly IProfileApiClient _profileApiClient;
    private readonly ILogger<CurrentUserAvatarService> _logger;
    
    private readonly IMemoryCache _cache;
    
    public CurrentUserAvatarService(
        IProfileHubService profileHub,
        IMediaLoaderService mediaLoaderService,
        IProfileApiClient profileApiClient,
        ILogger<CurrentUserAvatarService> logger,
        IMemoryCache cache)
    {
        _profileHub = profileHub;
        _mediaLoaderService = mediaLoaderService;
        _profileApiClient = profileApiClient;
        _logger = logger;
        _cache = cache;
    }
    
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<ImageSource> LoadAvatarAsync(Guid avatarId)
    {
        if (avatarId == Guid.Empty)
            return null;

        if (_cache.TryGetValue(avatarId, out byte[] cachedBytes))
        {
            return ImageSource.FromStream(() => new MemoryStream(cachedBytes));
        }

        var semaphore = _locks.GetOrAdd(avatarId, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();

        try
        {
            // double-check после входа в lock
            if (_cache.TryGetValue(avatarId, out cachedBytes))
            {
                return ImageSource.FromStream(() => new MemoryStream(cachedBytes));
            }

            var result = await _mediaLoaderService.Download(avatarId);

            if (!result.IsSuccess)
                return null;

            using var mem = new MemoryStream();
            await result.Value.FileStream.CopyToAsync(mem);
            var avatarBytes = mem.ToArray();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(20))
                .SetSize(avatarBytes.Length);

            _cache.Set(avatarId, avatarBytes, cacheOptions);

            return ImageSource.FromStream(() => new MemoryStream(avatarBytes));
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<Guid?> PickAndUploadNewAvatarAsync(Guid oldAvatarId)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите аватар",
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
                return null; 

            using var stream = await result.OpenReadAsync();
            
            var request = new AvatarUploadRequest()
            {
                FileStream = stream,
                FileName =  result.FileName,
                MimeType = $"image/{result.FileName.Split('.').Last()}", 
                Type = MediaType.Image,
            };

            var uploadResult = await _profileApiClient.UploadAvatar(request);
            
            if (!uploadResult.IsSuccess)
            {
                await AppShell.DisplayException("Ошибка при загрузке аватара на сервер.");
                return null;
            }
            
            var newMediaId = uploadResult.Value.MediaId;
            
            var hubResult =  await _profileHub.SetAvatarAsync(newMediaId);
            
            if (hubResult.Status != HubResultStatus.Success)
            {
                await AppShell.DisplayException("Не удалось установить картинку как аватар.");
                return null;
            }
            
            // Инвалидируем кеш 
            if (oldAvatarId != Guid.Empty)
                _cache.Remove(oldAvatarId);
            
            return newMediaId;
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка при выборе/загрузке аватара: {0}", ex.Message);
            await AppShell.DisplayException("Не удалось выбрать или установить аватар.");
            return null;
        }
    }
}