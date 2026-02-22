using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class UserAvatartVMCreater : IAvatartVMCreater
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProfileApiClient _profileApiClient;
    private readonly Dictionary<Guid, AvatarViewModel> _avatarViewModels = new Dictionary<Guid, AvatarViewModel>();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    
    public UserAvatartVMCreater(IServiceProvider serviceProvider, IProfileApiClient profileApiClient)
    {
        _serviceProvider = serviceProvider;
        _profileApiClient = profileApiClient;
    } 
    
    public async Task<AvatarViewModel> CreateAvatar(Guid userId)
    {
        // Сначала проверяем без блокировки
        if (_avatarViewModels.TryGetValue(userId, out var existing))
            return existing;

        // Только один поток создаёт аватарку одновременно
        await _refreshLock.WaitAsync();
        try
        {
            if (_avatarViewModels.TryGetValue(userId, out existing))
                return existing;

            var profile = await _profileApiClient.DowloadProfileByUserIdAsync(userId);
            var avatarVm = _serviceProvider.GetService<AvatarViewModel>();
            await avatarVm.InitializeAsync(profile.Username, profile.IconId);

            _avatarViewModels.Add(userId, avatarVm);
            return avatarVm;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}