using Govor.Mobile.Models;
using Govor.Mobile.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Data;

public class GovorDbContext : DbContext
{
    public DbSet<LocalUserProfile> Users;
    public DbSet<UserSession> CurrentSessions;

    public GovorDbContext(DbContextOptions<GovorDbContext> options) : base(options) { }
}
