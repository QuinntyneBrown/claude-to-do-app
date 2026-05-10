using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Account.DeleteAccount;

public sealed class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRequestContext _request;
    private readonly TimeProvider _clock;

    public DeleteMyAccountCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IRequestContext request,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _request = request;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        // Audit BEFORE deletion so the row's UserId still references a real account,
        // and so it survives the cascade (SecurityAuditEvents has no FK to Users).
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = SecurityAuditKind.AccountDeleted,
            OccurredAt = _clock.GetUtcNow(),
            IpAddress = _request.RemoteIp
        });

        // Explicitly delete child rows so the test InMemory provider behaves the
        // same as SQL Server (the relational cascade does this server-side).
        var todos = await _db.Todos.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
        _db.Todos.RemoveRange(todos);

        var refreshTokens = await _db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
        _db.RefreshTokens.RemoveRange(refreshTokens);

        var userRoles = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        _db.UserRoles.RemoveRange(userRoles);

        var resetTokens = await _db.PasswordResetTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
        _db.PasswordResetTokens.RemoveRange(resetTokens);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
