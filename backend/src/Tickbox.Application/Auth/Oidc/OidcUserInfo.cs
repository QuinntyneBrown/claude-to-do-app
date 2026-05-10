namespace Tickbox.Application.Auth.Oidc;

public sealed record OidcUserInfo(string Subject, string Email, string DisplayName);
