using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Auth.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, AuthenticationOutcome>
{
    private const string GenericFailure = "Refresh token is invalid.";

    private readonly IAppDbContext _db;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IJwtTokenService _tokens;

    public RefreshAccessTokenCommandHandler(
        IAppDbContext db,
        IRefreshTokenService refreshTokens,
        IJwtTokenService tokens)
    {
        _db = db;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
    }

    public async Task<AuthenticationOutcome> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokens.FindActiveAsync(request.RefreshToken, cancellationToken);
        if (existing is null)
        {
            throw new AuthenticationFailedException(GenericFailure);
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken)
                   ?? throw new AuthenticationFailedException(GenericFailure);

        await _refreshTokens.RevokeAsync(existing, cancellationToken);

        var roleNames = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _tokens.CreateAccessToken(user, roleNames);
        var (refreshPlain, refreshPersisted) = await _refreshTokens.IssueAsync(user.Id, cancellationToken);

        return new AuthenticationOutcome(user.Id, accessToken, refreshPlain, refreshPersisted.ExpiresAt);
    }
}
