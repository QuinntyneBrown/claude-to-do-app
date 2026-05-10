using Tickbox.Application.Auth.Oidc;

namespace Tickbox.Application.Common;

public interface IOidcClient
{
    Task<OidcUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);
}
