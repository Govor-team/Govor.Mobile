using CommunityToolkit.Maui;
using Govor.Mobile.Data;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.ContentViews;
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
        builder.Services.AddSingleton<IFriendsRequestQueryService, FriendsRequestQueryService>();
        builder.Services.AddSingleton<IFriendshipApiService, FriendshipApiService>();
        builder.Services.AddSingleton<IJwtProviderService, JwtProviderService>();

        builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
        builder.Services.AddSingleton<IBuilderDeviceInfoString, BuilderDeviceInfoString>();
        builder.Services.AddSingleton<IDeviceInfoParserService, DeviceInfoParserService>();

        builder.Services.AddSingleton<ISessionsService, SessionsService>();

        builder.Services.AddScoped<IUserProfileDonloaderSerivce, UserProfileDonloaderSerivce>();

        builder.Services.AddSingleton<IMediaLoaderService, MediaLoaderService>();

        builder.Services.AddSingleton<IServerIpProvader, ServerIpProvader>();
        builder.Services.AddSingleton<IPlatformIconService, PlatformIconService>();
        
        builder.Services.AddSingleton<IBackgroundImageService, BackgroundService>();
        
        builder.Services.AddSingleton<IWasOnlineFormater, WasOnlineFormater>();
        
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
        builder.Services.AddSingleton<PresenceHub>();
        builder.Services.AddSingleton<FriendsHub>();
        
        builder.Services.AddSingleton<ProfileHubListener>(); 
        
        builder.Services.AddSingleton<IProfileHubService>(sp => sp.GetRequiredService<ProfileHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<ProfileHub>());
        
        builder.Services.AddSingleton<IPresenceHubService>(sp => sp.GetRequiredService<PresenceHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<PresenceHub>());
        
        builder.Services.AddSingleton<IFriendsHubService>(sp => sp.GetRequiredService<FriendsHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<FriendsHub>());
        
        builder.Services.AddSingleton<IHubInitializer, HubInitializer>();

        // ViewModels 
        builder.Services.AddTransient<AvatarViewModel>();
        
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
        // Content View
        builder.Services.AddSingleton<BottomBar>();
        
        // Login Page
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<LoginPageModel>();

        builder.Services.AddSingleton<SomePage>();
        builder.Services.AddSingleton<SomePageModel>();

        builder.Services.AddSingleton<RegisterPage>();
        builder.Services.AddSingleton<RegisterPageModel>();

        builder.Services.AddSingleton<CodeInputPage>();
        builder.Services.AddSingleton<CodeInputModel>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<MainPageModel>();

        builder.Services.AddSingleton<FriendsSearchPage>();
        builder.Services.AddSingleton<FriendsSearchPageModel>();

        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsPageModel>();

        builder.Services.AddTransient<AuthShell>();
        builder.Services.AddSingleton<MainShell>();

        builder.Services.AddSingleton<AppShell>();


        return builder;
    }
}
