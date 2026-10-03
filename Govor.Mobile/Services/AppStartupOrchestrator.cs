using Govor.Mobile.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Govor.Mobile.Services;

public class AppStartupOrchestrator : IAppStartupOrchestrator
{
    private readonly ILogger<AppStartupOrchestrator> _logger;
    private readonly NetworkAvailabilityService  _networkAvailabilityService;
    private readonly IDbContextFactory<GovorDbContext> _dbContextFactory;
    
    public AppStartupOrchestrator(
        IDbContextFactory<GovorDbContext> dbContextFactory,
        NetworkAvailabilityService  networkAvailabilityService,
        ILogger<AppStartupOrchestrator> logger)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _networkAvailabilityService = networkAvailabilityService;
    }

    private readonly SemaphoreSlim _migrationLock = new(1, 1);
    private readonly HashSet<string> _readyDatabases = new();

    public async Task InitializeLocalAsync()
    {
        await _migrationLock.WaitAsync();
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var database = db.Database.GetConnectionString()!;
            if (_readyDatabases.Contains(database)) return;
            await db.Database.MigrateAsync();
            _readyDatabases.Add(database);
            _logger.LogInformation("Local database migration completed");
        }
        finally { _migrationLock.Release(); }
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting local database migration");

        await InitializeLocalAsync();

        _logger.LogInformation("Local database migration completed");
        
        _logger.LogInformation("Starting network availability service");
        await _networkAvailabilityService.CheckInitialConnectivityAsync();
        _logger.LogInformation("Network availability check completed");
    }
}
