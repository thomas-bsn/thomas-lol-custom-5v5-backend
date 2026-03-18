using Microsoft.EntityFrameworkCore;
using Custom5v5.Infrastructure.Entities;

namespace Custom5v5.Infrastructure.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>().ToTable("players");

        modelBuilder.Entity<User>().ToTable("users");

        // Relation 1-1 : un User a au plus un Player
        modelBuilder.Entity<User>()
            .HasOne(u => u.Player)
            .WithOne(p => p.User)
            .HasForeignKey<Player>(p => p.UserId);
    }
}