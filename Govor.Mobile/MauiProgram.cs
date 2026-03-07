#if ANDROID
using Plugin.FirebasePushNotifications.Platforms.Channels;
using Android.App;
#endif

using CommunityToolkit.Maui;
using CropperImage.MAUI;
using Indiko.Maui.Controls.Markdown;
using Sharpnado.MaterialFrame;
using Sharpnado.Tabs;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using UXDivers.Popups.Maui;
using Plugin.FirebasePushNotifications;
using Plugin.LocalNotification;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile
{
    public static class MauiProgram
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100 * 1024 * 1024; // ~100 MB
            });
            
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .UseFirebasePushNotifications(options =>
                {
                    // Создаём канал для чатов (Android) — очень важно для мессенджера!
#if ANDROID
                    options.Android.NotificationChannels = new[]
                    {
                        new NotificationChannelRequest
                        {
                            ChannelId          = "chat_messages",
                            ChannelName        = "Сообщения",
                            Description        = "Уведомления о новых сообщениях в чатах",
                            Importance         = NotificationImportance.High,
                            LockscreenVisibility      =  NotificationVisibility.Public,          
                            // VibrationPattern   = new long[] { 0, 250, 250, 250 },  // пример вибрации
                            // Sound              = "chat_sound",   // имя файла без расширения из Resources/raw
                            // PlaySound          = true,
                            // EnableVibration    = true,
                            // LockScreenVisibility = LockScreenVisibility.Public,
                        }
                    };
#elif IOS || MACCATALYST
    // если нужно что-то специфичное для iOS — добавь сюда
    // обычно для iOS каналы не нужны (FCM сам управляет)
#elif WINDOWS || LINUX
    //
#endif
                })
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
                // Govor Core:
                .RegisterDatabaseContext() 
                .RegisterHttpClients()
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
