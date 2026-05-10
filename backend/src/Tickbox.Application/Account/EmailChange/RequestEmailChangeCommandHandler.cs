using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Account.EmailChange;

public sealed class RequestEmailChangeCommandHandler : IRequestHandler<RequestEmailChangeCommand, MyProfile>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emails;
    private readonly TimeProvider _clock;

    public RequestEmailChangeCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IEmailService emails,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _emails = emails;
        _clock = clock;
    }

    public async Task<MyProfile> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        var normalisedNew = request.NewEmail.Trim().ToLowerInvariant();
        if (string.Equals(normalisedNew, user.Email, StringComparison.Ordinal))
        {
            throw new ValidationFailureException("newEmail", "The new email must differ from the current email.");
        }

        var alreadyTaken = await _db.Users.AnyAsync(u => u.Email == normalisedNew && u.Id != user.Id, cancellationToken);
        if (alreadyTaken)
        {
            throw new ValidationFailureException("newEmail", "That email is already in use.");
        }

        var plaintext = GenerateToken();
        var now = _clock.GetUtcNow();

        user.PendingEmail = normalisedNew;
        user.PendingEmailTokenHash = Hash(plaintext);
        user.PendingEmailExpiresAt = now + TokenLifetime;
        await _db.SaveChangesAsync(cancellationToken);

        await _emails.SendEmailChangeVerificationAsync(normalisedNew, plaintext, cancellationToken);

        return new MyProfile(user.Email, user.DisplayName, user.PendingEmail);
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
