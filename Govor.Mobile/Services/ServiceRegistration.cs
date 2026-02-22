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
using Govor.Mobile.Services.Implementations.ChatPage;
using Govor.Mobile.Services.Implementations.JwtServices;
using Govor.Mobile.Services.Implementations.MainPage;
using Govor.Mobile.Services.Implementations.Profiles;
using Govor.Mobile.Services.Implementations.Repositories;
using Govor.Mobile.Services.Interfaces;
using Govor.Mobile.Services.Interfaces.ChatPage;
using Govor.Mobile.Services.Interfaces.JwtServices;
using Govor.Mobile.Services.Interfaces.MainPage;
using Govor.Mobile.Services.Interfaces.Profiles;
using Govor.Mobile.Services.Interfaces.Repositories;
using Govor.Mobile.Services.Mapping;
using Microsoft.EntityFrameworkCore;
using MainPage = Govor.Mobile.Pages.MainFlow.MainPage;

namespace Govor.Mobile.Services;

internal static class ServiceRegistration
{
    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAppStartupOrchestrator, AppStartupOrchestrator>();
        
        builder.Services.AddSingleton<NetworkAvailabilityService>();
        
        builder.Services.AddSingleton<IApiClient, ApiClient>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IFriendsRequestQueryService, FriendsRequestQueryService>();
        builder.Services.AddSingleton<IFriendshipApiService, FriendshipApiService>();
        builder.Services.AddSingleton<IJwtProviderService, JwtProviderService>();
        builder.Services.AddSingleton<IChatLoaderApi, ChatLoaderApi>();

        builder.Services.AddSingleton<ITokenStorageService, TokenStorageService>();
        builder.Services.AddSingleton<IBuilderDeviceInfoString, BuilderDeviceInfoString>();
        builder.Services.AddSingleton<IDeviceInfoParserService, DeviceInfoParserService>();

        builder.Services.AddSingleton<ISessionsService, SessionsService>();

        builder.Services.AddScoped<IUserProfileDonloaderSerivce, UserProfileDonloaderSerivce>();

        builder.Services.AddSingleton<IMediaLoaderService, MediaLoaderService>();

        builder.Services.AddSingleton<IServerIpProvider, ServerIpProvider>();
        builder.Services.AddSingleton<IPlatformIconService, PlatformIconService>();
        
        builder.Services.AddSingleton<IBackgroundImageService, BackgroundService>();
        
        builder.Services.AddSingleton<IWasOnlineFormater, WasOnlineFormater>();
        
        builder.Services.AddSingleton<IFriendsListController, FriendsListController>();
        builder.Services.AddTransient<IFriendsFactory, FriendsFactory>();
        builder.Services.AddTransient<IFriendsRealtimeService, FriendsRealtimeService>();
        
        
        // Profiles
        builder.Services.AddSingleton<IProfileApiClient, ProfileApiClient>();
        builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
        
        builder.Services.AddSingleton<ICurrentUserAvatarService, CurrentUserAvatarService>();
        builder.Services.AddSingleton<IAvatartVMCreater, UserAvatartVMCreater>();
        
        builder.Services.AddSingleton<IDefaultAvatarGenerator, DefaultAvatarGenerator>();
        builder.Services.AddSingleton<IDescriptionService, DescriptionService>();
        builder.Services.AddSingleton<IMaxDescriptionLengthProvider, MaxDescriptionLengthProvider>();
        builder.Services.AddSingleton<IDeviceSessionManagerService, DeviceSessionManagerService>();

        builder.Services.AddSingleton<IMessagesRepository, MessagesRepository>(); // Message repository
        
        builder.Services.AddScoped<IAvatarStoragePath, AvatarStoragePathService>();
        builder.Services.AddScoped<IUserAvatarFileService, UserAvatarFileService>();
        builder.Services.AddScoped<IAvatarLoader, UserAvatarFileService>();
        builder.Services.AddScoped<IAvatarSaver, UserAvatarFileService>();
        
        // Chat
        builder.Services.AddSingleton<IChatHeaderService, ChatHeaderService>();
        builder.Services.AddTransient<IMessagesListController,  MessagesListController>();
        builder.Services.AddTransient<IMessageStore, MessageStore>();
        builder.Services.AddScoped<IPrivateChatApi, PrivateChatApi>();
        
        // Hubs
        builder.Services.AddSingleton<ProfileHub>();
        builder.Services.AddSingleton<PresenceHub>();
        builder.Services.AddSingleton<FriendsHub>();
        builder.Services.AddSingleton<ChatHub>();
        
        builder.Services.AddSingleton<ProfileHubListener>(); 
        
        builder.Services.AddSingleton<IProfileHubService>(sp => sp.GetRequiredService<ProfileHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<ProfileHub>());
        
        builder.Services.AddSingleton<IPresenceHubService>(sp => sp.GetRequiredService<PresenceHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<PresenceHub>());
        
        builder.Services.AddSingleton<IFriendsHubService>(sp => sp.GetRequiredService<FriendsHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<FriendsHub>());
        
        builder.Services.AddSingleton<IChatHub>(sp => sp.GetRequiredService<ChatHub>());
        builder.Services.AddSingleton<IHubClient>(sp => sp.GetRequiredService<ChatHub>());
        
        // Hub Initializer 
        builder.Services.AddSingleton<HubInitializer>();
        builder.Services.AddSingleton<IHubInitializer>(sp => sp.GetRequiredService<HubInitializer>());
        builder.Services.AddSingleton<IConnectivityChanged>(sp => sp.GetRequiredService<HubInitializer>());
       
        builder.Services.AddSingleton<UpdateService>();
        builder.Services.AddSingleton<IUpdateService>(sp => sp.GetRequiredService<UpdateService>());
        builder.Services.AddSingleton<IConnectivityChanged>(sp => sp.GetRequiredService<UpdateService>());
        
        // ViewModels 
        builder.Services.AddTransient<AvatarViewModel>();
        
        builder.Services.AddAutoMapper(typeof(MappingProfile));
        
        return builder;
    }

    public static MauiAppBuilder RegisterDatabaseContext(this MauiAppBuilder builder)
    {
        builder.Services.AddDbContextFactory<GovorDbContext>(options =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "govor.db");
            options.UseSqlite($"Data Source={dbPath}");
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
        builder.Services.AddSingleton<IConnectivityChanged>(sp => sp.GetRequiredService<MainPageModel>());
        
        builder.Services.AddSingleton<FriendsSearchPage>();
        builder.Services.AddSingleton<FriendsSearchPageModel>();

        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsPageModel>();

        builder.Services.AddSingleton<RootPage>();
        builder.Services.AddSingleton<RootPageViewModel>();

        builder.Services.AddTransient<ChatPageModel>();
        builder.Services.AddTransient<ChatPage>();
        
        builder.Services.AddTransient<AuthShell>();
        builder.Services.AddSingleton<MainShell>();

        builder.Services.AddSingleton<AppShell>();


        return builder;
    }
}
