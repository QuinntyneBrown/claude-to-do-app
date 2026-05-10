using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Account.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;
    private readonly IRequestContext _request;
    private readonly TimeProvider _clock;

    public ChangePasswordCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher hasher,
        IRequestContext request,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
        _request = request;
        _clock = clock;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        if (user.PasswordHash is null || !_hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _db.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Kind = SecurityAuditKind.SignInFailed,
                OccurredAt = now,
                IpAddress = _request.RemoteIp,
                Detail = "Change password — wrong current."
            });
            await _db.SaveChangesAsync(cancellationToken);
            throw new ValidationFailureException("currentPassword", "Current password is incorrect.");
        }

        user.PasswordHash = _hasher.Hash(request.NewPassword);

        var callerHash = string.IsNullOrEmpty(request.CallerRefreshToken) ? null : Hash(request.CallerRefreshToken);
        var others = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.TokenHash != callerHash)
            .ToListAsync(cancellationToken);
        foreach (var token in others)
        {
            token.RevokedAt = now;
        }

        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Kind = SecurityAuditKind.PasswordChanged,
            OccurredAt = now,
            IpAddress = _request.RemoteIp
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
