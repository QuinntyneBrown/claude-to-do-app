using MediatR;

namespace Tickbox.Application.Auth.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<AuthenticationOutcome>;
