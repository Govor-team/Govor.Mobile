using System.Collections.Concurrent;
using System.Reflection;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.Extensions.Caching.Memory;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class UserProfileService : IUserProfileService
{
    private readonly IProfileApiClient _apiClient;
    private readonly IPresenceHubService _presenceHubService;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _currentProfileLock = new(1, 1);
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private MemoryCacheEntryOptions GetMemoryOptions => new MemoryCacheEntryOptions()
        .SetSlidingExpiration(_cacheDuration)
        .SetSize(50);
    
    private string UserProfileCacheKey(Guid id) => $"UserProfile_{id}";
    public event Action<UserProfile>? OnProfileUpdated;
    private Guid _currentId;

    public UserProfileService(
        IProfileApiClient apiClient,
        IPresenceHubService presenceHubService,
        IMemoryCache memoryCache)
    {
        _apiClient = apiClient;
        _presenceHubService = presenceHubService;
        _cache = memoryCache;

        _presenceHubService.OnUserOnline += async userId =>
        {
            if (_cache.TryGetValue(UserProfileCacheKey(userId), out UserProfile cachedProfile))
            {
                cachedProfile.IsOnline = true;
                _cache.Set(UserProfileCacheKey(userId), cachedProfile, GetMemoryOptions);
            }
            else
            {
                await GetProfileAsync(userId);
            }
        };
    }

    public async Task<UserProfile> GetCurrentProfile()
    {
        if (_currentId != Guid.Empty)
            return await GetProfileAsync(_currentId);

        await _currentProfileLock.WaitAsync();
        
        try
        {
            if (_currentId != Guid.Empty)
                return await GetProfileAsync(_currentId);

            var profileDto = await _apiClient.DownloadCurrentAsync();
            if (profileDto == null) return null;

            var profile = MapDtoToProfile(profileDto);
            _currentId = profile.Id;
        
            _cache.Set(
                UserProfileCacheKey(_currentId), 
                profile, 
                GetMemoryOptions);

            return profile;
        }
        finally
        {
            _currentProfileLock.Release();
        }
    }

    public async Task<UserProfile> GetProfileAsync(Guid userId)
    {
        if (_cache.TryGetValue(UserProfileCacheKey(userId), out UserProfile cachedProfile))
            return cachedProfile;

        var profileDto = await _apiClient.DowloadProfileByUserIdAsync(userId);
        var profile = MapDtoToProfile(profileDto);

        _cache.Set(UserProfileCacheKey(userId), profile, GetMemoryOptions);
        return profile;
    }

    public async Task UpdateProfileFromHub(UserProfileDelta delta)
    {
        if (_cache.TryGetValue(UserProfileCacheKey(delta.UserId), out UserProfile existingProfile))
        {
            var changed = false;

            if (delta.Description != null)
            {
                existingProfile.Description = delta.Description;
                changed = true;
            }

            if (delta.IconId.HasValue && delta.IconId != Guid.Empty)
            {
                existingProfile.IconId = delta.IconId.Value;
                changed = true;
            }

            if (changed)
            {
                _cache.Set(UserProfileCacheKey(delta.UserId), existingProfile, GetMemoryOptions);
                OnProfileUpdated?.Invoke(existingProfile);
            }
        }
        else
        {
            OnProfileUpdated?.Invoke(await GetProfileAsync(delta.UserId));
        }
    }

    private UserProfile MapDtoToProfile(UserProfileDto dto) => new()
    {
        Id = dto.Id,
        Username = dto.Username,
        IconId = dto.IconId ?? Guid.Empty,
        Description = dto.Description ?? "",
        IsOnline = dto.IsOnline
    };
    public void Dispose()
    {
        _currentProfileLock.Dispose();
    }
}