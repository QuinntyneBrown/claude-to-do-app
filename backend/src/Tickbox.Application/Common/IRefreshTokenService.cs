using Tickbox.Domain;

namespace Tickbox.Application.Common;

public interface IRefreshTokenService
{
    /// <summary>Issues a new refresh token for the user, persists its hash, returns the plaintext.</summary>
    Task<(string Plaintext, RefreshToken Persisted)> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Looks up an unrevoked, unexpired refresh token by its plaintext value.</summary>
    Task<RefreshToken?> FindActiveAsync(string plaintext, CancellationToken cancellationToken);

    /// <summary>Marks the given refresh token as revoked.</summary>
    Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken);
}
