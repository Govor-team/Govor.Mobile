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
        IUserProfileService profileService)
    {
        _controller = controller;
        _profileService = profileService;
        
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
            if (!Friends.Contains(vm))
                Friends.Remove(vm);
        });
    }
    
    private void OnFriendAdded(UserListItemViewModel vm)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!Friends.Contains(vm))
                Friends.Add(vm);
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

        await Shell.Current.GoToAsync($"chat?chatId={item.UserId}&isGroup=false", animate: false);
    }
    
    public async Task OnInternetConnectedAsync()
    {
        var profile = await _profileService.GetCurrentProfile();
        Name = profile?.Username ?? "Гость";
        
        await _controller.InitializeAsync();
        
        var loaded = _controller.GetLoadedFriends();
        if (loaded.Any())
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Friends = new ObservableCollection<UserListItemViewModel>(loaded);
                OnPropertyChanged(nameof(Friends));
            });
        }
        
        IsLoaded = true;
    }

    public async Task OnInternetDisconnectedAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() => Name = "Соединение...");
    }

    public void Dispose()
    {
        _controller.FriendAdded -= OnFriendAdded;
        _controller.OnlineStatusChanged -= OnOnlineStatusChanged;
    }
}
