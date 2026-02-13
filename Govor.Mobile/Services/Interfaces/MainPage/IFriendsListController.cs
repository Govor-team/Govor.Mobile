using System.Collections.ObjectModel;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Services.Interfaces.MainPage;

public interface IFriendsListController : IDisposable
{
    public event Action? FriendsLoaded;           // опционально — для уведомления
    public event Action<UserListItemViewModel>? FriendAdded;
    public event Action<Guid, bool>? OnlineStatusChanged;
    Task InitializeAsync();
    public IReadOnlyList<UserListItemViewModel> GetLoadedFriends();
}