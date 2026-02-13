using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace Govor.Mobile
{
    // defoult @style/Maui.SplashTheme
    [Activity(Theme = "@style/Maui.SplashTheme", WindowSoftInputMode = SoftInput.AdjustResize, HardwareAccelerated = true, MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // 1. Говорим окну растянуться на весь экран (под бары)
            WindowCompat.SetDecorFitsSystemWindows(Window, false);
            
            // 2. Убираем системные цвета и принудительный контраст
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            
            Window.SetFlags(Android.Views.WindowManagerFlags.LayoutNoLimits,
                Android.Views.WindowManagerFlags.LayoutNoLimits);

            Window.SetFlags(Android.Views.WindowManagerFlags.HardwareAccelerated,
                Android.Views.WindowManagerFlags.HardwareAccelerated);
            
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                Window.NavigationBarContrastEnforced = false;
                Window.StatusBarContrastEnforced = false;
            }

            new ImageCropper.Maui.Platform().Init(this);
        }
    }
}
