using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class UserListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private AvatarViewModel avatar;

    [ObservableProperty]
    private TagViewModel tag;

    public readonly Guid UserId;
    public Guid ChatId { get; }
    public Guid FriendshipId;

    public UserListItemViewModel(AvatarViewModel avatar,
        TagViewModel tag, Guid userId = default, Guid friendshipId = default, Guid chatId = default)
    {
        UserId = userId;
        FriendshipId = friendshipId;
        ChatId = chatId;
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

    public DateTime? LastMessageSentAt { get; private set; }

    public void SetLastMessageSentAt(DateTime? value)
    {
        LastMessageSentAt = value;
    }

    // Actions
    [ObservableProperty]
    private bool showActions;
}