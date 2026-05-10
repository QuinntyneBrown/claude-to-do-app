using MediatR;
using Tickbox.Application.Common;

namespace Tickbox.Application.Auth.SignOut;

public sealed class SignOutCommandHandler : IRequestHandler<SignOutCommand, Unit>
{
    private readonly IRefreshTokenService _refreshTokens;

    public SignOutCommandHandler(IRefreshTokenService refreshTokens)
    {
        _refreshTokens = refreshTokens;
    }

    public async Task<Unit> Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var existing = await _refreshTokens.FindActiveAsync(request.RefreshToken, cancellationToken);
            if (existing is not null)
            {
                await _refreshTokens.RevokeAsync(existing, cancellationToken);
            }
        }

        return Unit.Value;
    }
}
