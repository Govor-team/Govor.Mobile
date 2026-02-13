using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.Models.Results;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Pages.ContentViews;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class FriendsSearchPageModel : ObservableObject, IDisposable, IInitializableViewModel
{
    private IServiceProvider _provider;
    
    [ObservableProperty]
    private ObservableCollection<UserListItemViewModel> _incomingRequests = new();

    [ObservableProperty]
    private ObservableCollection<UserListItemViewModel> _outgoingRequests = new();

    [ObservableProperty]
    private ObservableCollection<UserListItemViewModel> _searchResults = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ImageSource qrCodeImage;

    [ObservableProperty]
    private string _searchText;

    public bool IsLoaded { set; get; } = false;

    private Dictionary<Guid, UserListItemViewModel> _initedUsers = new();
    
    private readonly IFriendshipApiService _friendshipApiService;
    private readonly IFriendsRequestQueryService _queryService;
    private readonly IWasOnlineFormater _lastSeenFormater;
    private readonly IUserProfileService _profileService; 
    private readonly IProfileApiClient _profileApiClient;
    private readonly IFriendsHubService _friendsHubService;

    public FriendsSearchPageModel(
        IServiceProvider provider,
        IWasOnlineFormater formater,
        IFriendsHubService friendsHubService,
        IUserProfileService profileService,
        IProfileApiClient profileApiClient,
        IFriendsRequestQueryService queryService,
        IFriendshipApiService friendshipApiService)
    {
        _provider = provider;
        _friendsHubService = friendsHubService;
        _profileService = profileService;
        _profileApiClient = profileApiClient;
        _queryService = queryService;
        _lastSeenFormater = formater;
        _friendshipApiService = friendshipApiService;
    }
    
    public void Dispose()
    {
        _friendsHubService.FriendRequestReceived -= async (dto) => await OnRequestReceived(dto.RequesterId, dto.Id);
        _friendsHubService.YourFriendRequestReceived -= async (dto) => await OnYourRequestReceived(dto.AddresseeId, dto.Id);
        
        _friendsHubService.FriendRequestAccepted -= (dto) => OnRequestAccepted(dto.RequesterId);
        _friendsHubService.YourFriendRequestAccepted -= (dto) => OnYourRequestAccepted(dto.AddresseeId);
        
        _friendsHubService.FriendRequestRejected -= (dto) => OnRequestRejected(dto.RequesterId);
        _friendsHubService.YourFriendRequestRejected -= (dto) => OnYourRequestRejected(dto.AddresseeId);
        
        _initedUsers.Clear();
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
        
        _friendsHubService.FriendRequestReceived += async (dto) => await OnRequestReceived(dto.RequesterId, dto.Id);
        _friendsHubService.YourFriendRequestReceived += async (dto) => await OnYourRequestReceived(dto.AddresseeId, dto.Id);
        
        _friendsHubService.FriendRequestAccepted += (dto) =>  OnRequestAccepted(dto.RequesterId);
        _friendsHubService.YourFriendRequestAccepted += (dto) =>  OnYourRequestAccepted(dto.AddresseeId);
        
        _friendsHubService.FriendRequestRejected += (dto) =>  OnRequestRejected(dto.RequesterId);
        _friendsHubService.YourFriendRequestRejected += (dto) =>  OnYourRequestRejected(dto.AddresseeId);
        
        IsBusy = true;
        try 
        {
            await LoadRequestsAsync();
            IsLoaded = true;
        }
        finally 
        {
            IsBusy = false;
        }
    }

    #region Event Handlers
    
    private void OnYourRequestRejected(Guid AddresseeId)
    {
        if (_initedUsers.ContainsKey(AddresseeId))
        {
            OutgoingRequests.Remove(_initedUsers[AddresseeId]);
            _initedUsers.Remove(AddresseeId);
        }
    }

    private void OnRequestRejected(Guid RequesterId)
    {
        if (_initedUsers.ContainsKey(RequesterId))
        {
            IncomingRequests.Remove(_initedUsers[RequesterId]);
            _initedUsers.Remove(RequesterId);
        }
    }
    
    private void OnYourRequestAccepted(Guid AddresseeId)
    {
        if (_initedUsers.ContainsKey(AddresseeId))
        {
            OutgoingRequests.Remove(_initedUsers[AddresseeId]);
            _initedUsers.Remove(AddresseeId);
        }
    }

    private void OnRequestAccepted(Guid RequesterId)
    {
        if (_initedUsers.ContainsKey(RequesterId))
        {
            IncomingRequests.Remove(_initedUsers[RequesterId]);
            _initedUsers.Remove(RequesterId);
        }
    }

    private async Task OnYourRequestReceived(Guid AddresseeId, Guid Id)
    {
        if (!_initedUsers.ContainsKey(AddresseeId))
        {
            var profile = await _profileApiClient.DowloadProfileByUserIdAsync(AddresseeId);
            
            var vm = GetOrBuildProfile(profile);
            vm.FriendshipId = Id;
            
            SearchResults.Remove(vm);
            OutgoingRequests.Add(vm);
        }
    }

    private async Task OnRequestReceived(Guid RequesterId, Guid Id)
    {
        if (!_initedUsers.ContainsKey(RequesterId))
        {
            var profile = await _profileApiClient.DowloadProfileByUserIdAsync(RequesterId);
            
            var vm = GetOrBuildProfile(profile);
            vm.FriendshipId = Id;
            IncomingRequests.Add(vm);
        }
    }
    #endregion

    
    private async Task LoadRequestsAsync()
    {
        var incomingTask = _queryService.GetIncomingRequests();
        var outgoingTask = _queryService.GetResponses();

        await Task.WhenAll(incomingTask, outgoingTask);

        if (incomingTask.Result.IsSuccess)
        {
            var tasks = incomingTask.Result.Value
                .Select(async r =>
                {
                    var profile =
                        await _profileApiClient.DowloadProfileByUserIdAsync(r.RequesterId);

                    return (profile, r.Id); // friendshipId
                });

            var results = await Task.WhenAll(tasks);

            foreach (var (profile, friendshipId) in results)
            {
                var vm = GetOrBuildProfile(profile);
                vm.FriendshipId = friendshipId;

                if(!IncomingRequests.Contains(vm))
                    IncomingRequests.Add(vm);
            }
        }
        
        if (outgoingTask.Result.IsSuccess)
        {
            var tasks = outgoingTask.Result.Value
                .Select(async r =>
                {
                    var profile =
                        await _profileApiClient.DowloadProfileByUserIdAsync(r.AddresseeId);

                    return (profile, r.Id);
                });

            var results = await Task.WhenAll(tasks);

            foreach (var (profile, friendshipId) in results)
            {
                var vm = GetOrBuildProfile(profile);
                vm.FriendshipId = friendshipId;
                
                if (!OutgoingRequests.Contains(vm))
                    OutgoingRequests.Add(vm);
            }
        }
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
    public async Task OpenIncomingProfile(UserListItemViewModel user)
    {
        if(user is null)
            return;
        
        var model = new UserIncomingFriendPopupModel(_friendsHubService,
            user.UserId, user.FriendshipId)
        {
            Avatar = user.Avatar,
            Tag = user.Tag,
            Title = user.Title,
            Subtitle = user.Subtitle,
            IsOnline = user.IsOnline,
            OnlineText = user.DateTime
        };

        var popup = new UserIncomingFriendPopup(model);
        
        await IPopupService.Current.PushAsync(popup, waitUntilClosed: true);
    }
    
    [RelayCommand]
    public async Task OpenProfileAsync(UserListItemViewModel user)
    {
        if(user is null)
            return;
        
        var model = new UserAddNewFriendPopupModel(_friendsHubService, user.UserId)
        {
            Avatar = user.Avatar,
            Tag = user.Tag,
            Title = user.Title,
            Subtitle = user.Subtitle,
            IsOnline = user.IsOnline,
            OnlineText = user.DateTime
        };
        
        var popup = new UserAddNewFriendPopup(model);
        
        await IPopupService.Current.PushAsync(popup, waitUntilClosed: true);
        
    }
    

    
    [RelayCommand]
    private async Task SearchFriendsAsync(string query)
    {
        SearchResults.Clear();

        if (string.IsNullOrWhiteSpace(query)) 
        {
            IsSearching = false;
            return;
        }
        
    
        IsSearching = true;
        IsBusy = true;
    
        try 
        {
            var result = await _friendshipApiService.Search(query);

            if (result.IsSuccess)
            {
                foreach (var userDto in result.Value)
                {
                    var avatarViewModel = _provider.GetService<AvatarViewModel>();
                    avatarViewModel?.InitializeAsync(userDto.Username, userDto.IconId);
                    
                    SearchResults.Add(
                        new UserListItemViewModel(
                            userId: userDto.Id,
                            avatar: avatarViewModel,
                            tag: null)
                        {
                            Title = userDto.Username,
                            Subtitle = userDto.Description,
                            DateTime = _lastSeenFormater.FormatLastSeen(userDto.WasOnline),
                            IsOnline = userDto.IsOnline
                        });
                }
            }
        }
        finally 
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseSearch()
    {
        IsSearching = false;
        SearchResults.Clear();
    }
}

public partial class IncomingFriendsRequest : ObservableObject
{
    public AvatarViewModel AvatarViewModel { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public DateTime AccountCreatedAt { get; set; }
}