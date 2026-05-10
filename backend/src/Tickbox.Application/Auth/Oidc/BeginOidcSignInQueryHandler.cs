using System.Security.Cryptography;
using System.Text;
using MediatR;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Auth.Oidc;

public sealed class BeginOidcSignInQueryHandler : IRequestHandler<BeginOidcSignInQuery, BeginOidcSignInResult>
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(5);

    private readonly IAppDbContext _db;
    private readonly IOidcConfiguration _config;
    private readonly TimeProvider _clock;

    public BeginOidcSignInQueryHandler(IAppDbContext db, IOidcConfiguration config, TimeProvider clock)
    {
        _db = db;
        _config = config;
        _clock = clock;
    }

    public async Task<BeginOidcSignInResult> Handle(BeginOidcSignInQuery request, CancellationToken cancellationToken)
    {
        var verifier = GenerateRandomToken(64);
        var challenge = ToChallenge(verifier);
        var state = GenerateRandomToken(32);
        var now = _clock.GetUtcNow();

        _db.OidcAuthorizationRequests.Add(new OidcAuthorizationRequest
        {
            State = state,
            CodeVerifier = verifier,
            ExpiresAt = now + StateLifetime
        });
        await _db.SaveChangesAsync(cancellationToken);

        var authority = _config.Authority.TrimEnd('/');
        var url = $"{authority}/authorize"
                  + $"?client_id={Uri.EscapeDataString(_config.ClientId)}"
                  + $"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}"
                  + "&response_type=code"
                  + $"&scope={Uri.EscapeDataString(_config.Scopes)}"
                  + $"&state={Uri.EscapeDataString(state)}"
                  + $"&code_challenge={Uri.EscapeDataString(challenge)}"
                  + "&code_challenge_method=S256";

        return new BeginOidcSignInResult(url, state);
    }

    private static string GenerateRandomToken(int byteCount)
    {
        var bytes = new byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ToChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
