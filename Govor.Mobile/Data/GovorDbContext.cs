using Govor.Mobile.Models;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Data;

public class GovorDbContext : DbContext
{
    public DbSet<LocalUserProfile> Users;

    public GovorDbContext(DbContextOptions<GovorDbContext> options) : base(options) { }
}
