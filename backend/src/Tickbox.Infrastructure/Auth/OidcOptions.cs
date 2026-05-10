namespace Tickbox.Infrastructure.Auth;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    public bool Enabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "openid email profile";
}
