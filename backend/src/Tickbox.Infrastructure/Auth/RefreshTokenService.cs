using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tickbox.Application;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Infrastructure.Auth;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IAppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly RefreshTokenOptions _options;

    public RefreshTokenService(IAppDbContext db, TimeProvider clock, IOptions<RefreshTokenOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<(string Plaintext, RefreshToken Persisted)> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var plaintext = GenerateToken();
        var now = _clock.GetUtcNow();

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(plaintext),
            IssuedAt = now,
            ExpiresAt = now.AddDays(_options.LifetimeDays)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (plaintext, entity);
    }

    public async Task<RefreshToken?> FindActiveAsync(string plaintext, CancellationToken cancellationToken)
    {
        var hash = Hash(plaintext);
        var now = _clock.GetUtcNow();

        return await _db.RefreshTokens.SingleOrDefaultAsync(
            t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > now,
            cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        token.RevokedAt = _clock.GetUtcNow();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
