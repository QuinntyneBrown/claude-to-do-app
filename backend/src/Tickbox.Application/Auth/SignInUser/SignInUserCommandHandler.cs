using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.SignInUser;

public sealed class SignInUserCommandHandler : IRequestHandler<SignInUserCommand, SignInUserResult>
{
    private const string GenericFailure = "Incorrect email or password.";

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;

    public SignInUserCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<SignInUserResult> Handle(SignInUserCommand request, CancellationToken cancellationToken)
    {
        var normalisedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalisedEmail, cancellationToken);

        if (user is null || user.PasswordHash is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException(GenericFailure);
        }

        var roleNames = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var token = _tokens.CreateAccessToken(user, roleNames);
        return new SignInUserResult(user.Id, token);
    }
}
