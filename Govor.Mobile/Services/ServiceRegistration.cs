using CommunityToolkit.Maui;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;
using MainPage = Govor.Mobile.Pages.MainFlow.MainPage;

namespace Govor.Mobile.Services
{
    internal static class ServiceRegistration
    {
        public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IJwtProviderService, JwtProviderService>();
           
            builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
            builder.Services.AddSingleton<IBuilderDeviceInfoString, BuilderDeviceInfoString>();
            builder.Services.AddSingleton<IDeviceInfoParserService, DeviceInfoParserService>();

            builder.Services.AddSingleton<ISessionsService, SessionsService>();
            return builder;
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
            builder.Services.AddTransient<Govor.Mobile.PageModels.MainFlow.MainPageModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsPageModel>();

            builder.Services.AddTransient<AuthShell>();
            builder.Services.AddSingleton<MainShell>();

            builder.Services.AddSingleton<AppShell>();
            
            return builder;
        } 
    }
}
