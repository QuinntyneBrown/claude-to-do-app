using MediatR;

namespace Tickbox.Application.Auth.Oidc;

public sealed record CompleteOidcSignInCommand(string Code, string State) : IRequest<AuthenticationOutcome>;
