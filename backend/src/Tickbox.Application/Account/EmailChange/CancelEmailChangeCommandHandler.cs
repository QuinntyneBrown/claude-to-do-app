using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Account.EmailChange;

public sealed class CancelEmailChangeCommandHandler : IRequestHandler<CancelEmailChangeCommand, MyProfile>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelEmailChangeCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MyProfile> Handle(CancelEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        user.PendingEmail = null;
        user.PendingEmailTokenHash = null;
        user.PendingEmailExpiresAt = null;
        await _db.SaveChangesAsync(cancellationToken);

        return new MyProfile(user.Email, user.DisplayName, PendingEmail: null);
    }
}
