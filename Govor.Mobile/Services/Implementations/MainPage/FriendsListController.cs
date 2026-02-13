using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.MainPage;
using Govor.Mobile.Services.Interfaces.Repositories;
using CommunityToolkit.Mvvm.Collections;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services.Implementations.MainPage;

public class FriendsListController : IFriendsListController, IDisposable
{
    private readonly IFriendshipApiService _friendshipApi;
    private readonly IProfileApiClient _profileApi;
    private readonly IMessagesRepository _messages;
    private readonly IFriendsFactory _factory;
    private readonly IFriendsRealtimeService _realtime;

    private readonly ConcurrentDictionary<Guid, UserListItemViewModel> _cache = new();

    public event Action? FriendsLoaded;           // опционально — для уведомления
    public event Action<UserListItemViewModel>? FriendAdded;
    public event Action<Guid, bool>? OnlineStatusChanged;

    public FriendsListController(
        IFriendshipApiService friendshipApi,
        IProfileApiClient profileApi,
        IMessagesRepository messages,
        IFriendsFactory factory,
        IFriendsRealtimeService realtime)
    {
        _friendshipApi = friendshipApi;
        _profileApi = profileApi;
        _messages = messages;
        _factory = factory;
        _realtime = realtime;

        _realtime.OnUserOnline += id => OnlineStatusChanged?.Invoke(id, true);
        _realtime.OnUserOffline += id => OnlineStatusChanged?.Invoke(id, false);
        _realtime.OnFriendAdded += OnFriendAddedAsync;
        _realtime.OnUserAvatarUpdate += OnUserAvatarUpdateAsync;
    }

    public async Task InitializeAsync()
    {
        _messages.Initialize();

        var result = await _friendshipApi.GetFriends();
        if (!result.IsSuccess) return;

        await LoadFriendsProgressiveAsync(result.Value);

        FriendsLoaded?.Invoke();
    }
    
    private async Task LoadFriendsProgressiveAsync(IEnumerable<UserDto> friends)
    {
        if (friends == null || !friends.Any()) return;

        Console.WriteLine($"[DEBUG] Загружаем {friends.Count()} друзей");

        const int batchSize = 8;
        var validVms = new List<UserListItemViewModel>();

        int processed = 0;
        int success = 0;

        foreach (var batch in friends.Chunk(batchSize))
        {
            Console.WriteLine($"[DEBUG] Батч из {batch.Length} элементов");

            var tasks = batch.Select(async friend =>
            {
                processed++;
                try
                {
                    var profile = await _profileApi.DowloadProfileByUserIdAsync(friend.Id);
                    
                    if (profile == null)
                    {
                        Console.WriteLine($"[DEBUG] Profile null для {friend.Id}");
                        return null;
                    }

                    Console.WriteLine($"[DEBUG] Profile получен для {friend.Id} → {profile.Username}");

                    var vm = await _factory.CreateAsync(profile);
                    
                    if (vm == null)
                    {
                        Console.WriteLine($"[DEBUG] VM == null после фабрики для {friend.Id} ({profile.Username})");
                        return null;
                    }

                    _cache[profile.Id] = vm;
                    success++;
                    Console.WriteLine($"[DEBUG] Успешно создан VM для {friend.Id}");
                    return vm;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Исключение для {friend.Id}: {ex.Message}");
                    return null;
                }
            });

            var batchResults = await Task.WhenAll(tasks);

            var batchValid = batchResults.Where(x => x != null).ToList();

            Console.WriteLine($"[DEBUG] В батче успешно: {batchValid.Count}");

            if (batchValid.Any())
            {
                validVms.AddRange(batchValid);
            }
        }

        Console.WriteLine($"[SUMMARY] Обработано: {processed}, Успешно: {success}, В validVms: {validVms.Count}");
        Console.WriteLine($"[SUMMARY] В кэше: {_cache.Count}");
    }

    // Вызывается из ViewModel после загрузки
    public IReadOnlyList<UserListItemViewModel> GetLoadedFriends() => _cache.Values.ToList();

    private async Task OnFriendAddedAsync(Guid userId)
    {
        if (_cache.ContainsKey(userId)) return;

        try
        {
            var profile = await _profileApi.DowloadProfileByUserIdAsync(userId);
            if (profile == null) return;

            var vm = await _factory.CreateAsync(profile);
            if (vm == null) return;

            _cache[userId] = vm;
            FriendAdded?.Invoke(vm);
        }
        catch { /* log */ }
    }

    private async Task OnUserAvatarUpdateAsync(Guid userId, Guid avatarId)
    {
        if (_cache.TryGetValue(userId, out var vm))
        {
            await vm.Avatar.InitializeAsync(vm.Title, avatarId);
        }
    }

    public void UpdateOnlineStatus(Guid id, bool isOnline)
    {
        if (_cache.TryGetValue(id, out var vm))
        {
            vm.IsOnline = isOnline;
            OnlineStatusChanged?.Invoke(id, isOnline);
        }
    }

    public void Dispose()
    {
        _realtime.Dispose();
        // отписка от событий
        _realtime.OnFriendAdded -= OnFriendAddedAsync;
        _realtime.OnUserAvatarUpdate -= OnUserAvatarUpdateAsync;
    }
}
