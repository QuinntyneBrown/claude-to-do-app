using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.PasswordReset;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);

    private readonly IAppDbContext _db;
    private readonly IEmailService _emails;
    private readonly IRequestContext _request;
    private readonly TimeProvider _clock;

    public RequestPasswordResetCommandHandler(
        IAppDbContext db,
        IEmailService emails,
        IRequestContext request,
        TimeProvider clock)
    {
        _db = db;
        _emails = emails;
        _request = request;
        _clock = clock;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalisedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalisedEmail, cancellationToken);

        if (user is null)
        {
            return Unit.Value;
        }

        var now = _clock.GetUtcNow();
        var plaintext = GenerateToken();

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(plaintext),
            ExpiresAt = now + TokenLifetime
        });

        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Kind = SecurityAuditKind.PasswordResetRequested,
            OccurredAt = now,
            IpAddress = _request.RemoteIp
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _emails.SendPasswordResetAsync(user.Email, plaintext, cancellationToken);

        return Unit.Value;
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
