using CommunityToolkit.Maui;
using CropperImage.MAUI;
using Indiko.Maui.Controls.Markdown;

#if ANDROID
using Govor.Mobile.FloatingTabBar;
#endif

using Microsoft.Extensions.Logging;
using Sharpnado.MaterialFrame;
using Sharpnado.Tabs;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using UXDivers.Popups.Maui;
using Govor.Mobile.Data;

namespace Govor.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024; // лимит
            });
            
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(false);
                })
                .UseMauiCommunityToolkitMediaElement()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseUraniumUIBlurs() 
                .UseUXDiversPopups()
                .UseImageCropper()
                .ConfigureSyncfusionToolkit()
                .UseSharpnadoMaterialFrame(false)
                .UseSharpnadoTabs(loggerEnable: false)
                .UseSkiaSharp()
                .UseMarkdownView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                    fonts.AddFont("cyrillicold.ttf", "CirilicOld");
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
