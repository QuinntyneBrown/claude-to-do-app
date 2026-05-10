using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly TimeProvider _clock;

    public RegisterUserCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService tokens, TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalisedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == normalisedEmail, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("An account with that email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalisedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAt = _clock.GetUtcNow()
        };

        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = KnownRoles.UserRoleId });
        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokens.CreateAccessToken(user, new[] { KnownRoles.User });
        return new RegisterUserResult(user.Id, token);
    }
}
