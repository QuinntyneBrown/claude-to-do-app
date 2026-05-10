namespace Tickbox.Application.Auth.Oidc;

public sealed record BeginOidcSignInResult(string AuthorizationUrl, string State);
