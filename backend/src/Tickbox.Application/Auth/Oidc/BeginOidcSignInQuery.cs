using MediatR;

namespace Tickbox.Application.Auth.Oidc;

public sealed record BeginOidcSignInQuery() : IRequest<BeginOidcSignInResult>;
