using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Tickbox.Application.Auth.Oidc;
using Tickbox.Application.Common;

namespace Tickbox.Infrastructure.Auth;

public sealed class OidcClient : IOidcClient
{
    private readonly HttpClient _http;
    private readonly OidcOptions _options;

    public OidcClient(HttpClient http, IOptions<OidcOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<OidcUserInfo> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        var authority = _options.Authority.TrimEnd('/');
        var tokenEndpoint = $"{authority}/token";

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code_verifier"] = codeVerifier
            })
        };

        using var tokenResponse = await _http.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenEndpointResponse>(cancellationToken)
                    ?? throw new InvalidOperationException("OIDC token endpoint returned an empty body.");

        var userInfoEndpoint = $"{authority}/userinfo";
        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var userInfoResponse = await _http.SendAsync(userInfoRequest, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();
        var info = await userInfoResponse.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken)
                   ?? throw new InvalidOperationException("OIDC userinfo endpoint returned an empty body.");

        return new OidcUserInfo(info.Sub, info.Email, info.Name ?? info.Email);
    }

    private sealed record TokenEndpointResponse(string AccessToken);

    private sealed record UserInfoResponse(string Sub, string Email, string? Name);
}
