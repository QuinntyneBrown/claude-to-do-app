using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Account.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfile>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyProfileQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MyProfile> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        return new MyProfile(user.Email, user.DisplayName, user.PendingEmail);
    }
}
