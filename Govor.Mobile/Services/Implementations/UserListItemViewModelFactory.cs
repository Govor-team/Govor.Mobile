using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations;

public class UserListItemViewModelFactory : IUserListItemViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserProfileService _userProfileService;
    private readonly IWasOnlineFormater _lastSeenFormater;

    public UserListItemViewModelFactory(
        IServiceProvider serviceProvider,
        IUserProfileService userProfileService,
        IWasOnlineFormater lastSeenFormater)
    {
        _serviceProvider = serviceProvider;
        _lastSeenFormater = lastSeenFormater;
        _userProfileService = userProfileService;
    }

    public UserListItemViewModel Create(UserProfileDto profile)
    {
        var avatar = _serviceProvider.GetRequiredService<AvatarViewModel>();
        _ = avatar.InitializeAsync(profile.Username, profile.IconId);

        return new UserListItemViewModel(userId: profile.Id, avatar: avatar, tag: null)
        {
            Title = profile.Username,
            Subtitle = profile.Description ?? string.Empty,
            IsOnline = profile.IsOnline
        };
    }

    public async Task<UserListItemViewModel> CreateAsync(Guid userId)
    {
        var profile = await _userProfileService.GetProfileAsync(userId);
        var avatar = _serviceProvider.GetRequiredService<AvatarViewModel>();
        await avatar.InitializeAsync(profile.Username, profile.IconId);

        return new UserListItemViewModel(userId: profile.Id, avatar: avatar, tag: null)
        {
            Title = profile.Username,
            Subtitle = profile.Description ?? string.Empty,
            IsOnline = profile.IsOnline
        };
    }

    public async Task<UserListItemViewModel> CreateAsync(UserDto user)
    {
        var avatar = _serviceProvider.GetRequiredService<AvatarViewModel>();
        await avatar.InitializeAsync(user.Username, user.IconId);

        return new UserListItemViewModel(userId: user.Id, avatar: avatar, tag: null)
        {
            Title = user.Username,
            Subtitle = user.Description,
            DateTime = _lastSeenFormater.FormatLastSeen(user.WasOnline),
            IsOnline = user.IsOnline
        };
    }
}