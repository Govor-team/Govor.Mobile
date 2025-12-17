using System.Collections.Concurrent;
using System.Reflection;
using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class UserProfileService : IUserProfileService
{
    private readonly IProfileApiClient _apiClient;
    private readonly IProfileHubService _hubService;
    private readonly ConcurrentDictionary<Guid, UserProfile> _cache = new();

    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
    private readonly SemaphoreSlim _currentProfileLock = new(1, 1);

    public event Action<UserProfile>? OnProfileUpdated;
    
    private Guid _currentId;

    public UserProfileService(
        IProfileApiClient apiClient,
        IProfileHubService hubService)
    {
        _apiClient = apiClient;
        _hubService = hubService;
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

            if (profileDto == null)
                return null;

            var profile = new UserProfile
            {
                Id = profileDto.Id,
                Username = profileDto.Username,
                IconId = profileDto.IconId ?? Guid.Empty,
                Description = profileDto.Description ?? "",
                LastFetched = DateTime.UtcNow
            };

            _currentId = profile.Id;

            _cache[_currentId] = profile;

            return profile;
        }
        finally
        {
            _currentProfileLock.Release();
        }
    }

    public async Task<UserProfile> GetProfileAsync(Guid userId)
    {
        if (_cache.TryGetValue(userId, out var cachedProfile))
        {
            if ((DateTime.UtcNow - cachedProfile.LastFetched) < _cacheDuration)
            {
                return cachedProfile;
            }
        }

        // Запрос с сервера
        var profileDto = await _apiClient.DowloadProfileByUserIdAsync(userId);

        var profile = new UserProfile
        {
            Id = profileDto.Id,
            Username = profileDto.Username,
            IconId = profileDto.IconId ?? Guid.Empty,
            Description = profileDto.Description ?? "",
            LastFetched = DateTime.UtcNow
        };

        _cache[userId] = profile;
        return profile;
    }

    public async Task UpdateProfileFromHub(UserProfileDelta delta)
    {
        if (_cache.TryGetValue(delta.UserId, out var existingProfile))
        {
            var profileChanged = false;
            
            if (delta.Description != null)
            {
                existingProfile.Description = delta.Description;
                profileChanged = true;
            }

            if (delta.IconId.HasValue && delta.IconId != Guid.Empty)
            {
                existingProfile.IconId = delta.IconId.Value;
                profileChanged = true;
            }
            
            if (profileChanged)
            {
                existingProfile.LastFetched = DateTime.UtcNow;
                _cache[delta.UserId] = existingProfile;
                OnProfileUpdated?.Invoke(existingProfile);
            }
        }
        else
        {
            OnProfileUpdated?.Invoke(await GetProfileAsync(delta.UserId));
        }
    }
}