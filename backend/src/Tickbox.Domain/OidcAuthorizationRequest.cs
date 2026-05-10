namespace Tickbox.Domain;

public class OidcAuthorizationRequest
{
    public string State { get; set; } = string.Empty;
    public string CodeVerifier { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
