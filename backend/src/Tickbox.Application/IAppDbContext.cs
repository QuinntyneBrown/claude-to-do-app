using Microsoft.EntityFrameworkCore;
using Tickbox.Domain;

namespace Tickbox.Application;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Todo> Todos { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<SignInAttempt> SignInAttempts { get; }
    DbSet<SecurityAuditEvent> SecurityAuditEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
