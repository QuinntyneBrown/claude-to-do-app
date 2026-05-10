using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.SignInUser;

public sealed class SignInUserCommandHandler : IRequestHandler<SignInUserCommand, AuthenticationOutcome>
{
    private const string GenericFailure = "Incorrect email or password.";
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IRequestContext _request;
    private readonly TimeProvider _clock;

    public SignInUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        IRefreshTokenService refreshTokens,
        IRequestContext request,
        TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _request = request;
        _clock = clock;
    }

    public async Task<AuthenticationOutcome> Handle(SignInUserCommand request, CancellationToken cancellationToken)
    {
        var normalisedEmail = request.Email.Trim().ToLowerInvariant();
        var now = _clock.GetUtcNow();
        var windowStart = now - LockoutWindow;

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalisedEmail, cancellationToken);

        var recentFailures = await _db.SignInAttempts
            .Where(a => a.Email == normalisedEmail && a.OccurredAt >= windowStart && !a.Succeeded)
            .CountAsync(cancellationToken);

        if (recentFailures >= MaxAttempts)
        {
            await RecordAsync(normalisedEmail, succeeded: false, now, cancellationToken);
            await AuditAsync(user?.Id, SecurityAuditKind.SignInLocked, now, cancellationToken);
            throw new AuthenticationFailedException(GenericFailure);
        }

        if (user is null || user.PasswordHash is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            await RecordAsync(normalisedEmail, succeeded: false, now, cancellationToken);
            await AuditAsync(user?.Id, SecurityAuditKind.SignInFailed, now, cancellationToken);
            throw new AuthenticationFailedException(GenericFailure);
        }

        await RecordAsync(normalisedEmail, succeeded: true, now, cancellationToken);

        var roleNames = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _tokens.CreateAccessToken(user, roleNames);
        var (refreshPlain, refreshPersisted) = await _refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthenticationOutcome(user.Id, accessToken, refreshPlain, refreshPersisted.ExpiresAt);
    }

    private async Task RecordAsync(string email, bool succeeded, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _db.SignInAttempts.Add(new SignInAttempt
        {
            Id = Guid.NewGuid(),
            Email = email,
            OccurredAt = now,
            Succeeded = succeeded,
            IpAddress = _request.RemoteIp
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AuditAsync(Guid? userId, SecurityAuditKind kind, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            OccurredAt = now,
            IpAddress = _request.RemoteIp
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
