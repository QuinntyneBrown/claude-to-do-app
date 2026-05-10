using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Account.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandHandler : IRequestHandler<UpdateDisplayNameCommand, MyProfile>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateDisplayNameCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MyProfile> Handle(UpdateDisplayNameCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        user.DisplayName = request.DisplayName.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return new MyProfile(user.Email, user.DisplayName, user.PendingEmail);
    }
}
