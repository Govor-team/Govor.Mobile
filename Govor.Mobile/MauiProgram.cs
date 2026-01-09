using CommunityToolkit.Maui;
using CropperImage.MAUI;

#if ANDROID
using Govor.Mobile.FloatingTabBar;
#endif

using Microsoft.Extensions.Logging;
using Sharpnado.MaterialFrame;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using UXDivers.Popups.Maui;

namespace Govor.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
#pragma warning disable CA1416
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(false);
                })

                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID 
                    handlers.AddHandler<Shell, RoundedFloatingTabBarHandler>();
#endif
                })

                .UseMauiCommunityToolkitMediaElement()
#pragma warning restore CA1416
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseUraniumUIBlurs() 
                .UseUXDiversPopups()
                .UseImageCropper()
                .ConfigureSyncfusionToolkit()
                .UseSharpnadoMaterialFrame(false)
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                })
                .RegisterDatabaseContext()
                .RegisterAppServices()
                .RegisterAppPages();
#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif
            return builder.Build();
        }
    }
}
