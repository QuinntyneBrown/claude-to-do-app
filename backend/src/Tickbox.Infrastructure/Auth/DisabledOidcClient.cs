using Tickbox.Application.Auth.Oidc;
using Tickbox.Application.Common;

namespace Tickbox.Infrastructure.Auth;

/// <summary>
/// Registered when <c>Oidc:Enabled = false</c> so MediatR can resolve
/// <see cref="Tickbox.Application.Auth.Oidc.CompleteOidcSignInCommandHandler"/> at startup
/// even though the OIDC endpoints short-circuit to 404 before the handler is invoked.
/// </summary>
public sealed class DisabledOidcClient : IOidcClient
{
    public Task<OidcUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("OIDC is not enabled in this environment.");
    }
}
