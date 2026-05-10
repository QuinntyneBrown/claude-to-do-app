using Tickbox.Application.Auth.Oidc;
using Tickbox.Application.Common;

namespace Tickbox.Api.Tests;

public sealed class TestOidcClient : IOidcClient
{
    public static OidcUserInfo NextUser { get; set; } = new("oidc-sub-default", "oidc@example.com", "OIDC Default");

    public Task<OidcUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        return Task.FromResult(NextUser);
    }
}
