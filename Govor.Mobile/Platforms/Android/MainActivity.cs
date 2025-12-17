using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Govor.Mobile
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(Android.Views.WindowManagerFlags.LayoutNoLimits,
                Android.Views.WindowManagerFlags.LayoutNoLimits);

            Window.SetFlags(Android.Views.WindowManagerFlags.HardwareAccelerated,
                Android.Views.WindowManagerFlags.HardwareAccelerated);
            
            new ImageCropper.Maui.Platform().Init(this);
            
            base.OnCreate(savedInstanceState);
        }
    }
}
