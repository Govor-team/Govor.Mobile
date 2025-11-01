using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Interfaces;

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
            
            builder.Services.AddTransient<AuthShell>();
            builder.Services.AddSingleton<AppShell>();
            
            return builder;
        } 
    }
}
