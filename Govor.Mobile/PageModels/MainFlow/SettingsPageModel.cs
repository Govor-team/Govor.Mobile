using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;
using Govor.Mobile.ContentViews;
using Govor.Mobile.Models.Results;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.PageModels.MainFlow;

public partial class SettingsPageModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private AvatarViewModel avatarViewModel;
    
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

    private readonly IUserProfileService _profileService; 
    private readonly IDescriptionService _descriptionService;
    private readonly IDeviceSessionManagerService _sessionsService;
    private readonly int MaxLength;

    private Guid _currentUserId;
    
    public SettingsPageModel(
        IUserProfileService profileService,
        IDescriptionService descriptionService,
        IDeviceSessionManagerService sessionsService,
        IMaxDescriptionLengthProvider  maxDescriptionLengthProvider,
        AvatarViewModel avatarModel)
    {
        _profileService = profileService;
        _descriptionService = descriptionService;
        _sessionsService = sessionsService;
        MaxLength = maxDescriptionLengthProvider.MaxDescriptionLength;
        avatarViewModel = avatarModel;
    }
    
    public async Task InitAsync()
    {
        var profile = await _profileService.GetCurrentProfile();
        
        _profileService.OnProfileUpdated += userProfile =>
        {
            if (userProfile?.Id == _currentUserId)
                OnProfileUpdated(userProfile);
        };
        
        InitProfile(profile);
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

        AvatarViewModel.InitializeAsync(profile.Username, profile.IconId);
    }

    
    private async Task InitSessions()
    {
        var sessionModels = await _sessionsService.LoadSessionsAsync();

        Sessions.Clear();
        foreach (var model in sessionModels)
        {
            Sessions.Add(model); 
        }
    }
    
    [RelayCommand]
    private async Task RemoveSessionAsync(DeviceSession session)
    {
        bool success = await _sessionsService.CloseSessionAsync(session.Id);

        if (success)
        {
            Sessions.Remove(session);
        }
    }


    [RelayCommand]
    private async Task SwipedToCloseAsync()
    {
        await Shell.Current.GoToAsync("..");
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
        _profileService.OnProfileUpdated -= OnProfileUpdated;
    }
}
