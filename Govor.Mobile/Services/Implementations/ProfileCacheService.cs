using Govor.Mobile.Data;
using Govor.Mobile.Models;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations;

public class ProfileCacheService : IProfileCacheService
{
    private readonly IUserProfileDonloaderSerivce _donloaderSerivce;
    private readonly IMediaLoaderService _mediaLoader;
    private readonly ILogger<ProfileCacheService> _logger;
    private readonly GovorDbContext _context;
    private Models.LocalUserProfile? _cached;

    public ProfileCacheService(
        GovorDbContext context,
        IUserProfileDonloaderSerivce donloaderSerivce,
        IMediaLoaderService mediaLoader,
        ILogger<ProfileCacheService> logger)
    {
        _donloaderSerivce = donloaderSerivce;
        _mediaLoader = mediaLoader;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Возвращает локально сохранённый профиль.
    /// </summary>
    public async Task<Models.LocalUserProfile?> GetCurrentAsync()
    {
        throw new NotImplementedException();
        /*
        if (_cached != null && _cached.UserId == userId)
            return _cached;

        _cached = await _database._connection.Table<LocalProfile>()
            .Where(p => p.UserId == userId).FirstOrDefaultAsync();

        return _cached;*/
    }

    public async Task<LocalUserProfile> GetProfileByUserId(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<Models.LocalUserProfile?> RefreshAsync()
    {
        throw new NotImplementedException();
    }
}
