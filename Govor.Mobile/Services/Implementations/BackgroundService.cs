using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations
{
    public class BackgroundService : IBackgroundImageService
    {
        private const string BackgroundKey = "UserBackgroundImage";

        public string GetBackgroundImage()
        {
            // если пользователь выбрал фон — вернуть его
            if (Preferences.ContainsKey(BackgroundKey))
                return Preferences.Get(BackgroundKey, null);

            // иначе — выбрать по теме устройства
            var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
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
