using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.Oidc;

public sealed class CompleteOidcSignInCommandHandler : IRequestHandler<CompleteOidcSignInCommand, AuthenticationOutcome>
{
    private readonly IAppDbContext _db;
    private readonly IOidcClient _oidc;
    private readonly IOidcConfiguration _config;
    private readonly IJwtTokenService _tokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly TimeProvider _clock;

    public CompleteOidcSignInCommandHandler(
        IAppDbContext db,
        IOidcClient oidc,
        IOidcConfiguration config,
        IJwtTokenService tokens,
        IRefreshTokenService refreshTokens,
        TimeProvider clock)
    {
        _db = db;
        _oidc = oidc;
        _config = config;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<AuthenticationOutcome> Handle(CompleteOidcSignInCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        var stored = await _db.OidcAuthorizationRequests
            .SingleOrDefaultAsync(r => r.State == request.State, cancellationToken)
            ?? throw new ValidationFailureException("state", "OIDC state is invalid or has expired.");

        if (stored.ExpiresAt <= now)
        {
            _db.OidcAuthorizationRequests.Remove(stored);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ValidationFailureException("state", "OIDC state is invalid or has expired.");
        }

        var info = await _oidc.ExchangeCodeAsync(request.Code, stored.CodeVerifier, _config.RedirectUri, cancellationToken);

        _db.OidcAuthorizationRequests.Remove(stored);

        var normalisedEmail = info.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalisedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalisedEmail,
                DisplayName = string.IsNullOrWhiteSpace(info.DisplayName) ? normalisedEmail : info.DisplayName.Trim(),
                PasswordHash = null,
                CreatedAt = now
            };
            _db.Users.Add(user);
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = KnownRoles.UserRoleId });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var roleNames = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _tokens.CreateAccessToken(user, roleNames);
        var (refreshPlain, refreshPersisted) = await _refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthenticationOutcome(user.Id, accessToken, refreshPlain, refreshPersisted.ExpiresAt);
    }
}
