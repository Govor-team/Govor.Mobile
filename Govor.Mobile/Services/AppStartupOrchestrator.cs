using Govor.Mobile.Data;
using Govor.Mobile.Pages.AuthFlow;
using Govor.Mobile.Pages.MainFlow;
using Govor.Mobile.Services.Api;
using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services;

public class AppStartupOrchestrator : IAppStartupOrchestrator
{
    private readonly ILogger<AppStartupOrchestrator> _logger;
    private readonly NetworkAvailabilityService  _networkAvailabilityService;
    private readonly IBackgroundImageService _backgroundService;
    private readonly IServiceProvider _serviceProvider;
    
    public AppStartupOrchestrator(
        IServiceProvider serviceProvider,
        NetworkAvailabilityService  networkAvailabilityService,
        IBackgroundImageService backgroundService,
        ILogger<AppStartupOrchestrator> logger)
    {
        _logger = logger;
        _serviceProvider =  serviceProvider;
        _backgroundService = backgroundService;
        _networkAvailabilityService = networkAvailabilityService;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting network availability service");
            
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GovorDbContext>();
        await db.Database.MigrateAsync();   // применяет миграции, создаёт таблицы
        
        await _networkAvailabilityService.CheckInitialConnectivity();
    }
}