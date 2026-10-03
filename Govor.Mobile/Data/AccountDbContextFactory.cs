using Govor.Mobile.Services.Interfaces.JwtServices;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Data;

// Create options on each call: the signed-in account can change during this process.
public sealed class AccountDbContextFactory(IJwtProviderService session) : IDbContextFactory<GovorDbContext>
{
    public GovorDbContext CreateDbContext()
    {
        var account = session.CurrentUserId?.ToString("N") ?? "anonymous";
        var path = Path.Combine(FileSystem.AppDataDirectory, $"govor-{account}.db");
        return new GovorDbContext(new DbContextOptionsBuilder<GovorDbContext>()
            .UseSqlite($"Data Source={path}").Options);
    }

    public Task<GovorDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
