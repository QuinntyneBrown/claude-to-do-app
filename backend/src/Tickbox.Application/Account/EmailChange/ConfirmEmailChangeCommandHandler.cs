using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Account.EmailChange;

public sealed class ConfirmEmailChangeCommandHandler : IRequestHandler<ConfirmEmailChangeCommand, MyProfile>
{
    private const string GenericFailure = "The verification link is invalid or has expired.";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public ConfirmEmailChangeCommandHandler(IAppDbContext db, ICurrentUserService currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<MyProfile> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        var hash = Hash(request.Token);
        var now = _clock.GetUtcNow();

        if (user.PendingEmail is null
            || user.PendingEmailTokenHash != hash
            || user.PendingEmailExpiresAt is null
            || user.PendingEmailExpiresAt <= now)
        {
            throw new ValidationFailureException("token", GenericFailure);
        }

        user.Email = user.PendingEmail;
        user.PendingEmail = null;
        user.PendingEmailTokenHash = null;
        user.PendingEmailExpiresAt = null;
        await _db.SaveChangesAsync(cancellationToken);

        return new MyProfile(user.Email, user.DisplayName, PendingEmail: null);
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
