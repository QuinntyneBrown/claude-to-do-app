using Microsoft.Extensions.Options;
using Tickbox.Application.Auth.Oidc;

namespace Tickbox.Infrastructure.Auth;

public sealed class OidcConfigurationAdapter : IOidcConfiguration
{
    private readonly OidcOptions _options;

    public OidcConfigurationAdapter(IOptions<OidcOptions> options)
    {
        _options = options.Value;
    }

    public bool Enabled => _options.Enabled;
    public string Authority => _options.Authority;
    public string ClientId => _options.ClientId;
    public string RedirectUri => _options.RedirectUri;
    public string Scopes => _options.Scopes;
}
