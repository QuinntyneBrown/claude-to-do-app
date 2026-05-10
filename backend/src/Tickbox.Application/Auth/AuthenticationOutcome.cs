namespace Tickbox.Application.Auth;

public sealed record AuthenticationOutcome(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
