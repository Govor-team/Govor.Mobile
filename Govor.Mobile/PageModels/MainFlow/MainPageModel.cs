using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class MainPageModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string name = "Гость";
    
    [ObservableProperty]
    private ObservableCollection<UserListItemViewModel> _friends = new();
    
    public bool IsLoaded { get; private set; }
    
    private Dictionary<Guid, UserListItemViewModel> _initedUsers = new();

    private readonly IUserProfileService _profileCacheService;
    private readonly IFriendshipApiService _friendshipApiService;
    private readonly IProfileApiClient _profileApiClient;
    private readonly IUserProfileService _profileService; 
    private readonly IFriendsHubService _friendsHubService;
    private readonly IServiceProvider _provider;


    public MainPageModel(IUserProfileService profileCacheService,
        IFriendshipApiService friendshipApiService,
        IUserProfileService profileService,
        IProfileApiClient profileApiClient,
        IFriendsHubService friendsHubService,
        IServiceProvider provider)
    {
        _profileCacheService = profileCacheService;
        _profileService = profileService;
        _friendsHubService = friendsHubService;
        _friendshipApiService = friendshipApiService;
        _profileApiClient = profileApiClient;
        _provider = provider;
    }

    public async Task InitAsync()
    {
        if(IsLoaded)
            return;
        
        _profileService.OnProfileUpdated += userProfile =>
        {
            if (_initedUsers.ContainsKey(userProfile.Id))
                OnProfileUpdated(userProfile);
        };
        
        _friendsHubService.FriendRequestAccepted += (dto) =>  _ = OnRequestAccepted(dto.RequesterId);
        _friendsHubService.YourFriendRequestAccepted +=  (dto) => _ = OnYourFriendRequestAccepted(dto.AddresseeId);
        
        var currentProfile = await _profileCacheService.GetCurrentProfile();
        Name = currentProfile?.Username ?? Name;

        var friendsResult = await _friendshipApiService.GetFriends();

        if (!friendsResult.IsSuccess)
            return;

        var tasks = friendsResult.Value
            .Select(async r =>
            {
                var profile = await _profileApiClient.DowloadProfileByUserIdAsync(r.Id);

                return profile; // friendshipId
            });

        var results = await Task.WhenAll(tasks);

        foreach (var profile in results)
        {
            var vm = GetOrBuildProfile(profile);
            Friends.Add(vm);
        }

        IsLoaded = true;
    }

    private void AddProfileToViewList(UserProfileDto profile)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            var vm = GetOrBuildProfile(profile);
            if (!Friends.Contains(vm))
                Friends.Add(vm);
        });
    }
    
    private async Task OnYourFriendRequestAccepted(Guid addresseeId)
    {
        if (_initedUsers.ContainsKey(addresseeId))
            return;

        var profile = await _profileApiClient.DowloadProfileByUserIdAsync(addresseeId);
        AddProfileToViewList(profile);
    }
    
    private async Task OnRequestAccepted(Guid requesterId)
    {
        if (_initedUsers.ContainsKey(requesterId))
            return;
        
        var profile = await _profileApiClient.DowloadProfileByUserIdAsync(requesterId);
        AddProfileToViewList(profile);
    }
    
    private UserListItemViewModel GetOrBuildProfile(UserProfileDto profile)
    {
        if (_initedUsers.TryGetValue(profile.Id, out var existing))
            return existing;

        var vm = BuildByProfile(profile);
        _initedUsers[profile.Id] = vm;
        return vm;
    }
    
    private void OnProfileUpdated(UserProfile userProfile)
    {
        Application.Current?.Dispatcher.Dispatch(async () =>
        {
            var userView = _initedUsers[userProfile.Id];
            await userView.Avatar.InitializeAsync(userProfile.Username, userProfile.IconId);
        
            userView.Subtitle =  userProfile.Description;
            userView.Title = userProfile.Username;
            userView.IsOnline = userProfile.IsOnline;
        });
    }

    private UserListItemViewModel BuildByProfile(UserProfileDto profile)
    {
        var avatarViewModel = _provider.GetService<AvatarViewModel>();
        avatarViewModel?.InitializeAsync(profile.Username, profile.IconId);
        
        var userView = new UserListItemViewModel(
            avatarViewModel,
            null,
            profile.Id)
        {
            Title = profile.Username,
            Subtitle = profile.Description ?? string.Empty,
            IsOnline = profile.IsOnline,
        };
        
        _initedUsers[userView.UserId] = userView;
        
        return userView;
    }
    
    
    
    [RelayCommand]
    public async Task OpenChatWithUser(UserListItemViewModel user)
    {
        
    }
    
    [RelayCommand]
    private async Task SettingsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync($"{nameof(SettingsPage)}", true);
        }
        catch (Exception ex)
        {
            await AppShell.DisplayException($"{ex.Message}");
        }
    }

    public void Dispose()
    {
        _friendsHubService.FriendRequestAccepted -= (dto) =>  OnRequestAccepted(dto.RequesterId);
        _friendsHubService.YourFriendRequestAccepted -= (dto) =>  OnYourFriendRequestAccepted(dto.AddresseeId);
    }
}
