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

    public UserListItemViewModel(
        AvatarViewModel avatar,
        TagViewModel tag,
        int unreadCount = 0,
        Guid userId = default,
        Guid friendshipId = default,
        Guid chatId = default)
    {
        UserId = userId;
        FriendshipId = friendshipId;
        ChatId = chatId;
        Avatar = avatar;
        UnreadCount = unreadCount;
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

    [ObservableProperty]
    private int unreadCount;

    public bool HasUnreadMessages => UnreadCount > 0;

    partial void OnUnreadCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasUnreadMessages));
    }

    public DateTime? LastMessageSentAt { get; private set; }

    public void SetLastMessageSentAt(DateTime? value)
    {
        if (LastMessageSentAt == value) return;
        LastMessageSentAt = value;
        OnPropertyChanged(nameof(LastMessageSentAt));
    }

    [ObservableProperty]
    private bool showActions;
}