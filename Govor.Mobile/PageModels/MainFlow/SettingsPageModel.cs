using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Interfaces;
using UXDivers.Popups.Maui.Controls;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class SettingsPageModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private AvatarViewModel avatarViewModel;

    [ObservableProperty] 
    private TagViewModel tag;
    
    [ObservableProperty]
    private ObservableCollection<DeviceSession> sessions = new();

    [ObservableProperty]
    private string userName = "Гость";

    [ObservableProperty]
    private string about = "";
    
    [ObservableProperty]
    private bool hasChanges = false;
    private string _originalAbout = ""; 

    [ObservableProperty]
    private ImageSource avatarImage;

    [ObservableProperty]
    private string descriptionCounter = "0/500";

    [ObservableProperty]
    private Color descriptionCounterColor = Colors.White;

    [ObservableProperty]
    private bool isDescriptionValid = true;

    [ObservableProperty]
    private ObservableCollection<BackgroundItem> _backgroundItems = new();
    
    [ObservableProperty]
    private BackgroundItem _selectedBackground;

    [ObservableProperty]
    private string _versionOfApp = $"Alfa v{AppInfo.VersionString} - NAS";

    private readonly IUserProfileService _profileService; 
    private readonly IDescriptionService _descriptionService;
    private readonly IDeviceSessionManagerService _sessionsService;
    private readonly IBackgroundImageService _backgroundImageService;
    private readonly IAuthService _authService;
    private readonly int MaxLength;

    private Guid _currentUserId;
    private Action<UserProfile>? _profileUpdatedHandler;

    public SettingsPageModel(
        IUserProfileService profileService,
        IDescriptionService descriptionService,
        IDeviceSessionManagerService sessionsService,
        IMaxDescriptionLengthProvider  maxDescriptionLengthProvider,
        IBackgroundImageService backgroundImageService,
        IAuthService authService,
        AvatarViewModel avatarModel)
    {
        _profileService = profileService;
        _descriptionService = descriptionService;
        _sessionsService = sessionsService;
        _backgroundImageService = backgroundImageService;
        _authService = authService;
        MaxLength = maxDescriptionLengthProvider.MaxDescriptionLength;
        avatarViewModel = avatarModel;
    }
    
    public async Task InitAsync()
    {
        try
        {
            var profile = await _profileService.GetCurrentProfile();

            _profileUpdatedHandler = userProfile =>
            {
                if (userProfile?.Id == _currentUserId)
                    OnProfileUpdated(userProfile);
            };

            _profileService.OnProfileUpdated += _profileUpdatedHandler;


            InitProfile(profile);

            await InitBackgrounds();
            await InitSessions();
        }
        catch (Exception ex)
        {
            await AppShell.DisplayException("In init of settings page model:" + ex.Message);
        }
    }
    
    public async Task RefreshInit()
    {
        await InitSessions();
    }
    
    private void OnProfileUpdated(UserProfile obj)
    {
        Application.Current.Dispatcher.Dispatch(() =>
        {
            InitProfile(obj);
        });
    }

    private void InitProfile(UserProfile profile)
    {
        if (profile is null) return;

        _currentUserId = profile.Id;

        UserName = profile.Username;
        About = profile.Description;

        _originalAbout = About;
        UpdateDescriptionCounter();
        UpdateHasChanges();

        _ = AvatarViewModel.InitializeAsync(profile.Username, profile.IconId);
        
        Tag = new TagViewModel()
        {
            Text = "Govor+",
            Description = "Пользователь является админом говора",
            BodyColor = Color.FromArgb("#2A5BD7"),
            TextColor = Colors.White,
            Icon = ImageSource.FromFile("icon_vip.png"),
            StrokeColor = Color.FromArgb("#4F7DFF")
        };
        
        Tag.SetSolidShadow(Color.FromArgb("#0000FF"));
    }

    
    private async Task InitSessions()
    {
        var sessionModels = await _sessionsService.LoadSessionsAsync();

        Sessions.Clear();
        
        if(sessionModels is null)
            return;
        
        foreach (var model in sessionModels)
        {
            Sessions.Add(model); 
        }
    }
    
    private async Task InitBackgrounds()
    {
        var newItems = new ObservableCollection<BackgroundItem>();
    
        var backgrounds = _backgroundImageService.GetAvailableBackgrounds();
        foreach (var path in backgrounds)
        {
            newItems.Add(new BackgroundItem
            {
                Path = path.Path,
                IsSystem = path.IsSystem
            });
        }
        
        var current = _backgroundImageService.LoadCurrent();
        BackgroundItem selectedItem = null;

        if (current != null)
        {
            selectedItem = newItems.FirstOrDefault(b => b.Path == current.Path);
        }
        
        selectedItem ??= newItems.FirstOrDefault();
        
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SelectedBackground = null; 
            
            BackgroundItems = newItems;
            
            if (selectedItem != null)
            {
                SelectedBackground = selectedItem;
            }
        });
    }
    
    [RelayCommand]
    private async Task RemoveSessionAsync(DeviceSession session)
    {
        var popup = new SimpleActionPopup
        {
            Title = "Отключить текущую сессию?",
            Text = "Это действие нельзя отменить.",
            ActionButtonText = "Удалить",
            SecondaryActionButtonText = "Отмена",
            ActionButtonCommand = new Command(async () =>
            {
                await IPopupService.Current.PopAsync();
                
                if (session.IsCurrent)
                {
                    await _authService.LogoutAsync();
                    Application.Current?.Quit();
                }
                else
                {
                    bool success = await _sessionsService.CloseSessionAsync(session.Id);
                    
                    if (success)
                    {
                        Sessions.Remove(session);
                    }
                    else
                    {
                        await AppShell.DisplayException("Не удалось закрыть выбранную сессию!");
                    }
                }
            })
        };

        await IPopupService.Current.PushAsync(popup);
        
    }
    
    [RelayCommand]
    private async Task SwipedToCloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
    
    [RelayCommand]
    private void SelectionChanged(object selectedItem)
    {
        if (selectedItem is BackgroundItem item)
        {
            _backgroundImageService.ApplyBackground(item.Path);
            SelectedBackground = item;
        }
    }
    
    partial void OnSelectedBackgroundChanged(BackgroundItem value)
    {
        if (value is null)
            return;

        if (!string.IsNullOrEmpty(value.Path))
        {
            _backgroundImageService.ApplyBackground(value.Path);
        }
    }

    [RelayCommand]
    private async Task DeleteBackground(BackgroundItem backgroundItem)
    {
        if (string.IsNullOrEmpty(backgroundItem.Path)) return;
        
        if (backgroundItem.IsSystem)
        {
            await AppShell.DisplayException("Системные фоны нельзя удалить!");
            return;
        }
        
        var popup = new SimpleActionPopup
        {
            Title = "Удалить указанный фон?",
            Text = "Это действие нельзя отменить.",
            ActionButtonText = "Удалить",
            SecondaryActionButtonText = "Отмена",
            ActionButtonCommand = new Command(async () =>
            {
                await IPopupService.Current.PopAsync();
                
                int currentIndex = BackgroundItems.IndexOf(backgroundItem);
    
                var removed = await _backgroundImageService.RemoveBackgroundAsync(backgroundItem.Path);

                if (removed)
                {
                    bool wasSelected = (SelectedBackground == backgroundItem);
                    
                    BackgroundItems.Remove(backgroundItem);

                    if (wasSelected)
                    {
                        int nextIndex = Math.Max(0, currentIndex - 1);

                        if (BackgroundItems.Count > 0)
                        {
                            var nextBg = BackgroundItems[nextIndex];
                            
                            SelectedBackground = nextBg;
                            _backgroundImageService.ApplyBackground(nextBg.Path);
                        }
                        else
                        {
                            SelectedBackground = null;
                        }
                    }
                }
            })
        };

        await IPopupService.Current.PushAsync(popup);
    }
    
    [RelayCommand]
    private async Task PickOwnThemeFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите фоновое изображение",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "image/*" } },              
                    { DevicePlatform.iOS, new[] { "public.image" } },              
                    { DevicePlatform.WinUI, new[] { ".png", ".jpg", ".jpeg", ".gif" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.image" } }
                })
            });

            if (result != null)
            {
                var fromGallery = await _backgroundImageService.AddBackgroundFromGallery(result);

                if (!BackgroundItems.Any(b => b.Path == fromGallery.Path))
                {
                    BackgroundItems.Add(fromGallery);
                }
                
                _backgroundImageService.ApplyBackground(fromGallery.Path);
                SelectedBackground = fromGallery;
            }
        }
        catch (Exception ex)
        {
            await AppShell.DisplayException("Не удалось загрузить изображение");
        }
    }
    
    partial void OnAboutChanged(string oldValue, string newValue)
    {
        UpdateDescriptionCounter();
        UpdateHasChanges();
    }

    private void UpdateHasChanges()
    {
        HasChanges = (_originalAbout != About);
    }

    private void UpdateDescriptionCounter()
    {
        int length = About?.Length ?? 0;

        DescriptionCounter = $"{length}/{MaxLength}";
        IsDescriptionValid = length <= MaxLength;

        DescriptionCounterColor = IsDescriptionValid ? Colors.White : Colors.Red;
    }
    
    [RelayCommand]
    private async Task UpdateDescriptionAsync()
    {
        if (!HasChanges)
        {
            return; 
        }

        if (await _descriptionService.UpdateDescriptionAsync(About))
        {
            _originalAbout = About;
            UpdateHasChanges();
        }
    }
    
    public void Dispose()
    {
        _profileService.OnProfileUpdated -= _profileUpdatedHandler;
    }
}
