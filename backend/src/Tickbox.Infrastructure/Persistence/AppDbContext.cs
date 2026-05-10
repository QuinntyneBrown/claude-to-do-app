using Microsoft.EntityFrameworkCore;
using Tickbox.Application;
using Tickbox.Domain;

namespace Tickbox.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<SignInAttempt> SignInAttempts => Set<SignInAttempt>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<OidcAuthorizationRequest> OidcAuthorizationRequests => Set<OidcAuthorizationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).HasMaxLength(256);
            b.Property(u => u.PendingEmail).HasMaxLength(256);
            b.Property(u => u.PendingEmailTokenHash).HasMaxLength(128);
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

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).IsRequired().HasMaxLength(64);
            b.HasIndex(r => r.Name).IsUnique();
            b.HasData(new Role { Id = KnownRoles.UserRoleId, Name = KnownRoles.User });
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne<User>().WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SignInAttempt>(b =>
        {
            b.ToTable("SignInAttempts");
            b.HasKey(a => a.Id);
            b.Property(a => a.Email).IsRequired().HasMaxLength(256);
            b.Property(a => a.IpAddress).HasMaxLength(64);
            b.HasIndex(a => new { a.Email, a.OccurredAt });
        });

        modelBuilder.Entity<SecurityAuditEvent>(b =>
        {
            b.ToTable("SecurityAuditEvents");
            b.HasKey(e => e.Id);
            b.Property(e => e.Kind).HasConversion<int>();
            b.Property(e => e.IpAddress).HasMaxLength(64);
            b.Property(e => e.Detail).HasMaxLength(1024);
            b.HasIndex(e => new { e.UserId, e.OccurredAt });
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(t => t.Id);
            b.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);
            b.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(b =>
        {
            b.ToTable("PasswordResetTokens");
            b.HasKey(t => t.Id);
            b.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);
            b.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OidcAuthorizationRequest>(b =>
        {
            b.ToTable("OidcAuthorizationRequests");
            b.HasKey(r => r.State);
            b.Property(r => r.State).HasMaxLength(128);
            b.Property(r => r.CodeVerifier).IsRequired().HasMaxLength(256);
        });
    }
}
