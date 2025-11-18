using Govor.Mobile.Application.Services.Interfaces;
using Govor.Mobile.Application.Services.Interfaces.Wrappers;
using Microsoft.Maui.ApplicationModel;

namespace Govor.Mobile.Application.Services.Implementations
{
    public interface IApplication
    {
        AppTheme RequestedTheme { get; }
    }

    public class ApplicationWrapper : IApplication
    {
        public AppTheme RequestedTheme => Application.Current?.RequestedTheme ?? AppTheme.Light;
    }

    public class BackgroundService : IBackgroundImageService
    {
        private const string BackgroundKey = "UserBackgroundImage";
        private readonly IPreferences _preferences;

        public BackgroundService(IPreferences preferences)
        {
            _preferences = preferences;
        }

        public string GetBackgroundImage(IApplication application = null)
        {
            if (_preferences.ContainsKey(BackgroundKey))
                return _preferences.Get(BackgroundKey, null);

            var app = application ?? new ApplicationWrapper();
            var theme = app.RequestedTheme;
            return theme == AppTheme.Dark
                ? "background_girls.png"
                : "background_flag.png";
        }

        public void SetUserBackground(string imagePath)
        {
            _preferences.Set(BackgroundKey, imagePath);
        }

        public void ClearUserBackground()
        {
            _preferences.Remove(BackgroundKey);
        }
    }
}
