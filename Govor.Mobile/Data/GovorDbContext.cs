using Govor.Mobile.Data.Configurations;
using Govor.Mobile.Models;
using Govor.Mobile.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Data;

public class GovorDbContext : DbContext
{
    //public DbSet<LocalUserProfile> Users;
    public DbSet<LocalMessage> Messages { get; set; } 
    //public DbSet<UserSession> CurrentSessions { get; set; }

    public GovorDbContext(DbContextOptions<GovorDbContext> options) : base(options) 
    {
        Database.Migrate();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LocalMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
