using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Govor.Mobile.Data;

public class GovorDbContextFactory : IDesignTimeDbContextFactory<GovorDbContext>
{
    public GovorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GovorDbContext>();

        optionsBuilder.UseSqlite("Data Source=migration.db");

        return new GovorDbContext(optionsBuilder.Options);
    }
}