using Govor.Mobile.Application.Data;
using Govor.Mobile.Application.Models;
using Govor.Mobile.Application.Services.Api;
using Govor.Mobile.Application.Services.Interfaces;
using Govor.Mobile.Application.Services.Interfaces.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Govor.Mobile.Application.Services.Implementations;

public class ProfileCacheService : IProfileCacheService
{
    private readonly IUserProfileDonloaderSerivce _donloaderSerivce;
    private readonly IMediaLoaderService _mediaLoader;
    private readonly ILogger<ProfileCacheService> _logger;
    private readonly GovorDbContext _context;
    private LocalUserProfile? _cached;

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

    public async Task<LocalUserProfile?> GetCurrentAsync()
    {
        if (_cached != null)
        {
            return _cached;
        }

        _cached = await _context.UserProfiles.FirstOrDefaultAsync();
        return _cached;
    }

    public async Task<LocalUserProfile?> GetProfileByUserId(Guid userId)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile != null)
        {
            return profile;
        }

        var result = await _donloaderSerivce.GetProfileByUserId(userId);
        if (result.IsSuccess)
        {
            var userProfile = result.Value;
            var newProfile = new LocalUserProfile
            {
                UserId = userId,
                DisplayName = userProfile.DisplayName,
                About = userProfile.About,
                AvatarPath = string.Empty
            };
            await _context.UserProfiles.AddAsync(newProfile);
            await _context.SaveChangesAsync();
            return newProfile;
        }

        return null;
    }

    public async Task<LocalUserProfile?> RefreshAsync()
    {
        var result = await _donloaderSerivce.GetProfile();
        if (result.IsSuccess)
        {
            var userProfile = result.Value;
            var existingProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userProfile.UserId);
            if (existingProfile != null)
            {
                existingProfile.DisplayName = userProfile.DisplayName;
                existingProfile.About = userProfile.About;
            }
            else
            {
                existingProfile = new LocalUserProfile
                {
                    UserId = userProfile.UserId,
                    DisplayName = userProfile.DisplayName,
                    About = userProfile.About,
                    AvatarPath = string.Empty
                };
                await _context.UserProfiles.AddAsync(existingProfile);
            }

            await _context.SaveChangesAsync();
            _cached = existingProfile;
            return existingProfile;
        }

        return null;
    }
}
