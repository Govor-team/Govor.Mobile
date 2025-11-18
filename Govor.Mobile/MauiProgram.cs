using CommunityToolkit.Maui;
using Govor.Mobile.Application.Services;
using Govor.Mobile.PageModels;
using Govor.Mobile.PageModels.AuthFlow;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Microsoft.Extensions.Logging;
using Sharpnado.MaterialFrame;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Govor.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(false);
                })
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
                .RegisterDatabaseContext() // From Govor.Mobile.Application
                .RegisterAppServices()     // From Govor.Mobile.Application
                .RegisterAppPages();       // Local to Govor.Mobile
#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif
            return builder.Build();
        }

        public static MauiAppBuilder RegisterAppPages(this MauiAppBuilder builder)
        {
            // Login Page
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginPageModel>();

            builder.Services.AddTransient<SomePage>();
            builder.Services.AddTransient<SomePageModel>();

            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterPageModel>();

            builder.Services.AddTransient<CodeInputPage>();
            builder.Services.AddTransient<CodeInputModel>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainPageModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsPageModel>();

            builder.Services.AddTransient<AuthShell>();
            builder.Services.AddSingleton<MainShell>();

            builder.Services.AddSingleton<AppShell>();

            return builder;
        }
    }
}
