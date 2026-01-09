using Govor.Mobile.Models.Requests;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class CurrentUserAvatarService : ICurrentUserAvatarService
{
    private readonly IProfileHubService _profileHub;
    private readonly IMediaLoaderService _mediaLoaderService;
    private readonly IProfileApiClient _profileApiClient;
    private readonly IUserAvatarFileService _fileService;
    private readonly ILogger<CurrentUserAvatarService> _logger;

    public CurrentUserAvatarService(IProfileHubService profileHub,
        IMediaLoaderService mediaLoaderService,
        IProfileApiClient profileApiClient,
        IUserAvatarFileService avatarFileService,
        ILogger<CurrentUserAvatarService> logger)
    {
        _profileHub = profileHub;
        _mediaLoaderService = mediaLoaderService;
        _profileApiClient = profileApiClient;
        _fileService = avatarFileService;
        _logger = logger;
    }

    public async Task<ImageSource> LoadAvatarAsync(Guid avatarId)
    {
        if (avatarId == Guid.Empty)
        {
            return null; 
        }

        var localImage = await _fileService.LoadLocalAvatarAsync(avatarId);

        if (localImage != null)
        {
            _logger?.LogInformation("Avatar loaded from local cache: {AvatarId}", avatarId);
            return localImage;
        }
        
        var result = await _mediaLoaderService.Download(avatarId);

        if (!result.IsSuccess)
        {
            return default;
        }
        
        var mem = new MemoryStream();
        await result.Value.FileStream.CopyToAsync(mem);
        mem.Position = 0;
        
        _ = Task.Run(async () =>
        {
            mem.Seek(0, SeekOrigin.Begin); 
            await _fileService.SaveAvatarAsync(avatarId,
                $"dummy{result.Value.FileName}.{result.Value.MimeType.Split("/")[1]}", // получение имени файла и его расширения
                mem);
            mem.Dispose();
        });
        
        mem.Seek(0, SeekOrigin.Begin);
        
        return ImageSource.FromStream(() =>
        {
            return new MemoryStream(mem.ToArray());
        });
    }

    public async Task<Guid?> PickAndUploadNewAvatarAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите аватар",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null)
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
            
            var mediaId = uploadResult.Value.MediaId;
            
            await _profileHub.SetAvatarAsync(mediaId);
            
            return mediaId;
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка при выборе/загрузке аватара: {0}", ex.Message);
            await AppShell.DisplayException("Не удалось выбрать или установить аватар.");
            return null;
        }
    }
}