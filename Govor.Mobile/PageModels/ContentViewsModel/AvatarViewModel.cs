using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Govor.Mobile.ContentViews;
using Govor.Mobile.Pages.ContentViews;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.Profiles;
using UXDivers.Popups.Services;

namespace Govor.Mobile.PageModels.ContentViewsModel;

public partial class AvatarViewModel : ObservableObject
{
    private readonly ICurrentUserAvatarService _profileService;
    private readonly IDefaultAvatarGenerator _avatarGenerator;

    [ObservableProperty]
    private ImageSource? avatarImage;

    [ObservableProperty]
    private string avatarText = string.Empty;

    [ObservableProperty]
    private Color avatarBackgroundColor = Colors.Gray;
    
    public bool HasImage => AvatarImage != null;

    public AvatarViewModel(
        ICurrentUserAvatarService profileService,
        IDefaultAvatarGenerator avatarGenerator)
    {
        _profileService = profileService;
        _avatarGenerator = avatarGenerator;
    }

    public async Task InitializeAsync(string? userName, Guid? iconId)
    {
        ApplyDefaultAvatar(userName);
        
        if (iconId.HasValue && iconId.Value != Guid.Empty)
        {
           await TryLoadAvatarAsync(iconId.Value);
        }
    }

    private void ApplyDefaultAvatar(string? userName)
    {
        var nameForAvatar = (userName ?? string.Empty).Trim();

        var avatar = _avatarGenerator.GenerateAvatar(nameForAvatar);

        AvatarText = avatar.Text;
        AvatarBackgroundColor = avatar.BackgroundColor;
        AvatarImage = null;           
    }

    private async Task TryLoadAvatarAsync(Guid avatarId)
    {
        try
        {
            var image = await _profileService.LoadAvatarAsync(avatarId);

            if (image != null)
            {
                AvatarImage = image;
                AvatarBackgroundColor = Colors.Transparent;
            }
        }
        catch
        {
            // fallback уже установлен через ApplyDefaultAvatar
        }
    }

    [RelayCommand]
    private async Task PickNewAvatarAsync()
    {
        var newMediaId = await _profileService.PickAndUploadNewAvatarAsync();

        if (!newMediaId.HasValue)
            return;

        await TryLoadAvatarAsync(newMediaId.Value);
        await AppShell.DisplayInfo("Успешно","Аватар успешно обновлён!");
    }

    [RelayCommand]
    private async Task OpenAvatarAsync()
    {
        if (AvatarImage == null)
            return;

        await IPopupService.Current.PushAsync(new ImagePreviewPopup(AvatarImage));
    }
}