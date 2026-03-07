using System.Collections.Concurrent;
using Govor.Mobile.Models.DTO;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.MainPage;
using Govor.Mobile.Services.Interfaces.Repositories;

namespace Govor.Mobile.Services.Implementations.MainPage;

public class FriendsListController : IFriendsListController
{
    private readonly IFriendshipApiService _friendshipApi;
    private readonly IProfileApiClient _profileApi;
    private readonly IMessagesRepository _messages;
    private readonly IFriendsFactory _factory;
    private readonly IFriendsRealtimeService _realtime;
    private readonly IPrivateChatApi _privateChatApi;
    
    private readonly ConcurrentDictionary<Guid, UserListItemViewModel> _cache = new();

    public event Action? FriendsLoaded;           
    public event Action<UserListItemViewModel>? FriendAdded;
    public event Action<UserListItemViewModel>? FriendRemoved;
    public event Action<Guid, bool>? OnlineStatusChanged;

    public FriendsListController(
        IFriendshipApiService friendshipApi,
        IProfileApiClient profileApi,
        IPrivateChatApi privateChatApi,
        IMessagesRepository messages,
        IFriendsFactory factory,
        IFriendsRealtimeService realtime)
    {
        _friendshipApi = friendshipApi;
        _profileApi = profileApi;
        _messages = messages;
        _factory = factory;
        _privateChatApi = privateChatApi;
        _realtime = realtime;

        // Подписка на события реального времени
        _realtime.OnUserOnline += id => OnlineStatusChanged?.Invoke(id, true);
        _realtime.OnUserOffline += id => OnlineStatusChanged?.Invoke(id, false);
        _realtime.OnFriendAdded += OnFriendAddedAsync;
        _realtime.OnFriendRemoved += OnFriendRemovedAsync;
        _realtime.OnUserAvatarUpdate += OnUserAvatarUpdateAsync;
    }

    public async Task InitializeAsync()
    {
        _messages.Initialize();

        var friendsResult = await _friendshipApi.GetFriends();
        var privateChatsResult = await _privateChatApi.GetPrivateChats();

        if (!friendsResult.IsSuccess || !privateChatsResult.IsSuccess)
            return;

        await LoadFriendsAsync(friendsResult.Value, privateChatsResult.Value);

        FriendsLoaded?.Invoke();
    }

    private async Task LoadFriendsAsync(IEnumerable<UserDto> friends, IEnumerable<PrivateChatDto> privateChats)
    {
        if (friends == null || !friends.Any()) return;

        const int batchSize = 8;
        int processed = 0;
        int success = 0;

        foreach (var batch in friends.Chunk(batchSize))
        {
            var tasks = batch.Select(async friend =>
            {
                processed++;
                try
                {
                    var profile = await _profileApi.DowloadProfileByUserIdAsync(friend.Id);
                    if (profile == null) return null;

                    // Находим приватный чат
                    var chat = privateChats.FirstOrDefault(c => c.FriendId == friend.Id); 
                    
                    var vm = await _factory.CreateAsync(profile, chat?.ChatId ?? Guid.Empty);

                    if (vm != null)
                    {
                        _cache[profile.Id] = vm;
                        success++;
                    }

                    return vm;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Для {friend.Id}: {ex.Message}");
                    return null;
                }
            });

            await Task.WhenAll(tasks);
        }

        Console.WriteLine($"[SUMMARY] Обработано: {processed}, Успешно: {success}, В кэше: {_cache.Count}");
    }
    
    // Вызывается из ViewModel после загрузки
    public IReadOnlyList<UserListItemViewModel> GetLoadedFriends() => _cache.Values.ToList();

    private async Task OnFriendAddedAsync(Guid userId)
    {
        try
        {
            if (_cache.ContainsKey(userId)) return;

            var profile = await _profileApi.DowloadProfileByUserIdAsync(userId);
            var privateChatRusult = await _privateChatApi.GetChatByFriendId(userId);
            
            if (profile is null || !privateChatRusult.IsSuccess) return;

            var vm = await _factory.CreateAsync(profile, privateChatRusult.Value);
            if (vm == null) return;

            if (_cache.TryAdd(userId, vm))
            {
                FriendAdded?.Invoke(vm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FriendsListController] Ошибка OnFriendAddedAsync: {ex}");
        }
    }

    private async Task OnFriendRemovedAsync(Guid userId)
    {
        try
        {
            if (_cache.TryRemove(userId, out var vm) && vm is not null)
            {
                FriendRemoved?.Invoke(vm);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FriendsListController] Ошибка OnFriendRemovedAsync: {ex}");
        }
    }
    
    private async Task OnUserAvatarUpdateAsync(Guid userId, Guid avatarId)
    {
        if (_cache.TryGetValue(userId, out var vm))
        {
            try
            {
                await vm.Avatar.InitializeAsync(vm.Title, avatarId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FriendsListController] Ошибка OnUserAvatarUpdateAsync: {ex}");
            }
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
    
    private void UnsubscribeRealtimeEvents()
    {
        _realtime.OnUserOnline -= id => OnlineStatusChanged?.Invoke(id, true);
        _realtime.OnUserOffline -= id => OnlineStatusChanged?.Invoke(id, false);
        _realtime.OnFriendAdded -= OnFriendAddedAsync;
        _realtime.OnFriendRemoved -= OnFriendRemovedAsync;
        _realtime.OnUserAvatarUpdate -= OnUserAvatarUpdateAsync;
    }

    public void Dispose()
    {
        UnsubscribeRealtimeEvents();
        _realtime.Dispose();
    }
}
