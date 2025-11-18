using Govor.Mobile.Services.Interfaces;
using Microsoft.Maui.ApplicationModel;

namespace Govor.Mobile.Services.Implementations
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

        public string GetBackgroundImage(IApplication application = null)
        {
            if (Preferences.ContainsKey(BackgroundKey))
                return Preferences.Get(BackgroundKey, null);

            var app = application ?? new ApplicationWrapper();
            var theme = app.RequestedTheme;
            return theme == AppTheme.Dark
                ? "background_girls.png"
                : "background_flag.png";
        }

        public void SetUserBackground(string imagePath)
        {
            Preferences.Set(BackgroundKey, imagePath);
        }

        public void ClearUserBackground()
        {
            Preferences.Remove(BackgroundKey);
        }
    }
}
