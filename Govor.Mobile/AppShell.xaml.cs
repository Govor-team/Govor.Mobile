using UXDivers.Popups.Maui.Controls;
using UXDivers.Popups.Services;
using Font = Microsoft.Maui.Font;

namespace Govor.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        /*public static async Task DisplaySnackbarAsync(string message)
        {
            if (OperatingSystem.IsWindows())
                return;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF3300"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(0),
                Font = Font.SystemFontOfSize(18),
                ActionButtonFont = Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);

            await snackbar.Show(cancellationTokenSource.Token);
        }*/
        
        public static async Task DisplayException(string message)
        {
            var popup = new FloaterPopup()
            {
                Title = "Ошибка",
                Text = message,
                IconColor = Colors.Red,
                IconText = "!",
            };

            await IPopupService.Current.PushAsync(popup);
        }
    
        public static async Task DisplayInfo(string title, string message)
        {
            var popup = new FloaterPopup()
            {
                Title = title,
                Text = message,
                IconColor = Colors.Green,
                IconText = "!",
            };
            
            await IPopupService.Current.PushAsync(popup);
        }
        
        private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
        }
    }
}
