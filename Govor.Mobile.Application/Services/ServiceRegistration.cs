using CommunityToolkit.Maui;
using Govor.Mobile.Application.Data;
using Govor.Mobile.Application.Services.Api;
using Govor.Mobile.Application.Services.Api.Base;
using Govor.Mobile.Application.Services.Implementations;
using Govor.Mobile.Application.Services.Implementations.JwtServices;
using Govor.Mobile.Application.Services.Implementations.Profiles;
using Govor.Mobile.Application.Services.Interfaces;
using Govor.Mobile.Application.Services.Interfaces.JwtServices;
using Govor.Mobile.Application.Services.Interfaces.Profiles;
using Govor.Mobile.Application.Services.Interfaces.Wrappers;
using Govor.Mobile.Application.Services.Implementations.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Application.Services;

public static class ServiceRegistration
{
    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<HttpClient>();
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

        // Profiles
        builder.Services.AddScoped<IAvatarStoragePath, AvatarStoragePathService>();
        builder.Services.AddScoped<IAvatarSaver, AvatarSaverService>();

        // Wrappers
        builder.Services.AddSingleton<IPreferences, PreferencesWrapper>();
        builder.Services.AddSingleton<ISecureStorage, SecureStorageWrapper>();

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
}
