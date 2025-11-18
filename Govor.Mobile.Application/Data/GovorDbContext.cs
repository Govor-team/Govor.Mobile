using Govor.Mobile.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Govor.Mobile.Application.Data;

public class GovorDbContext : DbContext
{
    public DbSet<LocalUserProfile> Users;

    public GovorDbContext(DbContextOptions<GovorDbContext> options) : base(options) { }
}
