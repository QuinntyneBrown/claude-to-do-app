namespace Tickbox.Application.Auth.Oidc;

public interface IOidcConfiguration
{
    bool Enabled { get; }
    string Authority { get; }
    string ClientId { get; }
    string RedirectUri { get; }
    string Scopes { get; }
}
