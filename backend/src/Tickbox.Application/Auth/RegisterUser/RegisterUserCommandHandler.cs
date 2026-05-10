using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthenticationOutcome>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly TimeProvider _clock;

    public RegisterUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        IRefreshTokenService refreshTokens,
        TimeProvider clock)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<AuthenticationOutcome> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
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

        var accessToken = _tokens.CreateAccessToken(user, new[] { KnownRoles.User });
        var (refreshPlain, refreshPersisted) = await _refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthenticationOutcome(user.Id, accessToken, refreshPlain, refreshPersisted.ExpiresAt);
    }
}
