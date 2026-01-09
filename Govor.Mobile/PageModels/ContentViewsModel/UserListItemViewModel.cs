using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class UserListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private AvatarViewModel avatar;
    
    [ObservableProperty]
    private TagViewModel tag;

    public readonly Guid UserId;
    public Guid FriendshipId;
    
    public UserListItemViewModel(AvatarViewModel avatar,
        TagViewModel tag, Guid userId = default, Guid friendshipId = default)
    {
        UserId = userId;
        FriendshipId = friendshipId;
        Avatar = avatar;
        Tag = tag;
    }
    
    [ObservableProperty]
    private bool isOnline;
    
    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string subtitle;

    [ObservableProperty]
    private string dateTime;

    // Actions
    [ObservableProperty]
    private bool showActions;
}