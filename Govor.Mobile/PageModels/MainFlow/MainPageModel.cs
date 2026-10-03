using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSharpMath.Structures;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Interfaces.MainPage;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class MainPageModel : ObservableObject, IInitializableViewModel, IConnectivityChanged, IDisposable
{
    public ObservableCollection<UserListItemViewModel> Friends { private set; get; } = new();

    [ObservableProperty]
    private string name;

    private readonly IFriendsListController _controller;
    private readonly IUserProfileService _profileService;

    public MainPageModel(
        IFriendsListController controller,
        IUserProfileService profileService,
        IJwtProviderService session)
    {
        _controller = controller;
        _profileService = profileService;
        session.WasClearTokens += () => MainThread.BeginInvokeOnMainThread(() =>
        {
            Friends.Clear();
            Name = "Гость";
            IsLoaded = false;
        });
        
        _controller.FriendAdded += OnFriendAdded;
        _controller.FriendRemoved += OnFriendRemoved;
        _controller.OnlineStatusChanged += OnOnlineStatusChanged;
    }

    public bool IsLoaded { get; set; }

    public async Task InitAsync()
    {
        if(!IsLoaded)
            await OnInternetConnectedAsync();
    }
    
    private void OnFriendRemoved(UserListItemViewModel vm)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Friends.Contains(vm))
            {
                vm.PropertyChanged -= FriendActivityChanged;
                Friends.Remove(vm);
            }
        });
    }
    
    private void OnFriendAdded(UserListItemViewModel vm)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!Friends.Contains(vm))
            {
                Friends.Add(vm);
                vm.PropertyChanged += FriendActivityChanged;
                SortFriends();
            }
        });
    }

    private void OnOnlineStatusChanged(Guid id, bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var friend = Friends.FirstOrDefault(f => f.UserId == id);
            if (friend != null)
                friend.IsOnline = isOnline;
        });
    }

    [RelayCommand]
    private async Task SettingsAsync()
    {
        if (Shell.Current == null)
        {
            await LogExceptionAsync("Shell.Current == null", "SettingsAsync");
            await AppShell.DisplayException("Shell.Current == null");
            return;
        }
    
        try
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
        catch (Exception ex)
        {
            await LogExceptionAsync(ex, "SettingsAsync");
            await AppShell.DisplayException(ex.ToString());
        }
    }
    
    /// <summary>
    /// Логирование исключений в файл
    /// </summary>
    private async Task LogExceptionAsync(Exception ex, string methodName)
    {
        try
        {
            var log = new StringBuilder();
            log.AppendLine("=== Ошибка ===");
            log.AppendLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"Метод: {methodName}");
            log.AppendLine($"Сообщение: {ex.Message}");
            log.AppendLine($"Источник: {ex.Source}");
            log.AppendLine($"Стек-трейс: {ex.StackTrace}");
    
            if (ex.InnerException != null)
            {
                log.AppendLine("--- Внутреннее исключение ---");
                log.AppendLine($"Сообщение: {ex.InnerException.Message}");
                log.AppendLine($"Стек-трейс: {ex.InnerException.StackTrace}");
            }
    
            // Путь к папке загрузок (кроссплатформенно)
            string folderPath;
    
    #if ANDROID
            folderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
    #elif IOS
            folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    #elif WINDOWS
            folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    #else
            folderPath = FileSystem.AppDataDirectory;
    #endif
    
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
    
            string fileName = $"Govor_Error_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(folderPath, fileName);
    
            await File.WriteAllTextAsync(filePath, log.ToString());
            Debug.WriteLine($"[LOG] Ошибка сохранена в: {filePath}");
        }
        catch
        {
            // Если лог не удаётся записать — молча игнорируем, чтобы не ломать приложение
        }
    }
    
    /// <summary>
    /// Перегрузка для строки
    /// </summary>
    private Task LogExceptionAsync(string message, string methodName)
    {
        return LogExceptionAsync(new Exception(message), methodName);
    }

    [RelayCommand]
    private async Task OpenChatWithUser(UserListItemViewModel item)
    {
        if (item == null)
        {
            //_logger.LogWarning("OpenChatWithUser called with null item");
            return;
        }

        if (item.ChatId == Guid.Empty)
            return;
        await Shell.Current.GoToAsync($"chat?chatId={item.ChatId}&peerId={item.UserId}&isGroup=false", animate: false);
    }
    
    public async Task OnInternetConnectedAsync()
    {
        var profile = await _profileService.GetCurrentProfileAsync();
        await MainThread.InvokeOnMainThreadAsync(() => Name = profile?.Username ?? "Гость");
        
        await _controller.InitializeAsync();
        
        var loaded = _controller.GetLoadedFriends();
        if (loaded != null)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ReplaceFriends(loaded);
                OnPropertyChanged(nameof(Friends));
            });
        }
        
        IsLoaded = true;
    }

    public async Task OnInternetDisconnectedAsync()
    {
        await _controller.LoadLocalAsync();
        var loaded = _controller.GetLoadedFriends();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Name = "Нет соединения";
            ReplaceFriends(loaded);
            OnPropertyChanged(nameof(Friends));
        });
    }

    private void ReplaceFriends(IEnumerable<UserListItemViewModel> loaded)
    {
        foreach (var f in Friends) f.PropertyChanged -= FriendActivityChanged;
        Friends = new ObservableCollection<UserListItemViewModel>(loaded);
        foreach (var f in Friends) f.PropertyChanged += FriendActivityChanged;
        SortFriends();
    }
    private void FriendActivityChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserListItemViewModel.LastMessageSentAt)) MainThread.BeginInvokeOnMainThread(SortFriends);
    }
    private void SortFriends()
    {
        var ordered = Friends.OrderByDescending(f => f.LastMessageSentAt).ThenBy(f => f.UserId).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var oldIndex = Friends.IndexOf(ordered[i]);
            if (oldIndex != i) Friends.Move(oldIndex, i);
        }
    }
    public void Dispose()
    {
        foreach (var f in Friends) f.PropertyChanged -= FriendActivityChanged;
        _controller.FriendRemoved -= OnFriendRemoved;
        _controller.FriendAdded -= OnFriendAdded;
        _controller.OnlineStatusChanged -= OnOnlineStatusChanged;
    }
}
