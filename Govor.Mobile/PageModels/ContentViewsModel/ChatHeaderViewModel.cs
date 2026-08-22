using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class ChatHeaderViewModel : ObservableObject
{
    [ObservableProperty] private string title;
    [ObservableProperty] private string subtitle;
    [ObservableProperty] private AvatarViewModel avatar;
    [ObservableProperty] private bool isOnline;
    [ObservableProperty] private bool isGroup;
    public required Guid Id { get; init; } 

    public IAsyncRelayCommand GoBackCommand { get; set; }
    public IAsyncRelayCommand CallCommand { get; set; }
    public IAsyncRelayCommand OpenMenuCommand { get; set; }
    public IAsyncRelayCommand LeaveCommand { get; set; }
}