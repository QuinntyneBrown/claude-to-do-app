using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Auth;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.PasswordReset;

public sealed class CompletePasswordResetCommandHandler : IRequestHandler<CompletePasswordResetCommand, AuthenticationOutcome>
{
    private const string GenericFailure = "The reset link is invalid or has expired.";

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IRequestContext _request;
    private readonly TimeProvider _clock;

    public CompletePasswordResetCommandHandler(
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

    public async Task<AuthenticationOutcome> Handle(CompletePasswordResetCommand request, CancellationToken cancellationToken)
    {
        var hash = Hash(request.Token);
        var now = _clock.GetUtcNow();

        var resetToken = await _db.PasswordResetTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (resetToken is null || resetToken.ConsumedAt is not null || resetToken.ExpiresAt <= now)
        {
            throw new ValidationFailureException("token", GenericFailure);
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken)
                   ?? throw new ValidationFailureException("token", GenericFailure);

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        resetToken.ConsumedAt = now;

        var existingRefreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in existingRefreshTokens)
        {
            token.RevokedAt = now;
        }

        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Kind = SecurityAuditKind.PasswordResetUsed,
            OccurredAt = now,
            IpAddress = _request.RemoteIp
        });

        await _db.SaveChangesAsync(cancellationToken);

        var roleNames = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _tokens.CreateAccessToken(user, roleNames);
        var (refreshPlain, refreshPersisted) = await _refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthenticationOutcome(user.Id, accessToken, refreshPlain, refreshPersisted.ExpiresAt);
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
