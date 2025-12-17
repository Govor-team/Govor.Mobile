using CommunityToolkit.Maui;
using Govor.Mobile.Data;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Implementations;
using Govor.Mobile.Services.Implementations.JwtServices;
using Govor.Mobile.Services.Implementations.Profiles;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Services.Interfaces.Profiles;
using Microsoft.EntityFrameworkCore;
using MainPage = Govor.Mobile.Pages.MainFlow.MainPage;

namespace Govor.Mobile.Services;

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

        builder.Services.AddScoped<IUserProfileDonloaderSerivce, UserProfileDonloaderSerivce>();

        builder.Services.AddSingleton<IMediaLoaderService, MediaLoaderService>();

        builder.Services.AddSingleton<IServerIpProvader, ServerIpProvader>();
        builder.Services.AddSingleton<IPlatformIconService, PlatformIconService>();

        // Profiles
        builder.Services.AddSingleton<IProfileApiClient, ProfileApiClient>();
        builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
        
        builder.Services.AddScoped<ICurrentUserAvatarService, CurrentUserAvatarService>();
    
        builder.Services.AddSingleton<IDefaultAvatarGenerator, DefaultAvatarGenerator>();
        builder.Services.AddSingleton<IDescriptionService, DescriptionService>();
        builder.Services.AddSingleton<IMaxDescriptionLengthProvider, MaxDescriptionLengthProvider>();
        builder.Services.AddSingleton<IDeviceSessionManagerService, DeviceSessionManagerService>();
        
        builder.Services.AddScoped<IAvatarStoragePath, AvatarStoragePathService>();
        builder.Services.AddScoped<IUserAvatarFileService, UserAvatarFileService>();
        builder.Services.AddScoped<IAvatarLoader, UserAvatarFileService>();
        builder.Services.AddScoped<IAvatarSaver, UserAvatarFileService>();

        // Hubs
        builder.Services.AddSingleton<ProfileHub>();
        
        builder.Services.AddSingleton<ProfileHubListener>(); 
        
        builder.Services.AddSingleton<IProfileHubService>(sp => sp.GetRequiredService<ProfileHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<ProfileHub>());
        builder.Services.AddSingleton<IHubInitializer, HubInitializer>();

        // ViewModels 
        builder.Services.AddScoped<AvatarViewModel>();
        
        return builder;
    }

    public static MauiAppBuilder RegisterDatabaseContext(this MauiAppBuilder builder)
    {
        builder.Services.AddDbContext<GovorDbContext>(optionsAction =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "govor.db");
            optionsAction.UseSqlite($"Data Source = {dbPath}");
        });
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

        builder.Services.AddTransient<FriendsSearchPage>();
        builder.Services.AddTransient<FriendsSearchPageModel>();
        
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SettingsPageModel>();

        builder.Services.AddTransient<AuthShell>();
        builder.Services.AddSingleton<MainShell>();

        builder.Services.AddSingleton<AppShell>();

        return builder;
    }
}
