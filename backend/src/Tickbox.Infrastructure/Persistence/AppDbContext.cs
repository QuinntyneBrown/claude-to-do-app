using Microsoft.EntityFrameworkCore;
using Tickbox.Application;
using Tickbox.Domain;

namespace Tickbox.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<Todo>(b =>
        {
            b.ToTable("Todos");
            b.HasKey(t => t.Id);
            b.Property(t => t.Title).IsRequired().HasMaxLength(200);
            b.Property(t => t.Status).HasConversion<int>();
            b.HasIndex(t => new { t.UserId, t.CreatedAt });
            b.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
