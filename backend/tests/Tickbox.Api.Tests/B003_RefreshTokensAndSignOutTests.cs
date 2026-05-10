using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;

namespace Tickbox.Api.Tests;

public sealed class B003_RefreshTokensAndSignOutTests : IClassFixture<TickboxApiFactory>
{
    private const string CookieName = "tickbox.refresh";

    private readonly TickboxApiFactory _factory;

    public B003_RefreshTokensAndSignOutTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Refresh_rotates_token_and_returns_new_access_jwt()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "rotation@example.com",
            displayName = "Rotation Tester",
            password = "correct-horse-battery-staple"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstCookie = ExtractRefreshCookie(registerResponse);
        firstCookie.Should().NotBeNull();
        var firstAccessToken = (await registerResponse.Content.ReadFromJsonAsync<RegisterUserResult>())!.AccessToken;

        await Task.Delay(1100);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"{CookieName}={firstCookie}");
        var refreshResponse = await client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<RegisterUserResult>();
        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.AccessToken.Should().NotBe(firstAccessToken);

        var rotatedCookie = ExtractRefreshCookie(refreshResponse);
        rotatedCookie.Should().NotBeNull();
        rotatedCookie.Should().NotBe(firstCookie);
    }

    [Fact]
    public async Task Refresh_with_revoked_token_returns_401()
    {
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "revoked@example.com",
            displayName = "Revoked Tester",
            password = "correct-horse-battery-staple"
        });
        var originalCookie = ExtractRefreshCookie(register);

        using var firstRefresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRefresh.Headers.Add("Cookie", $"{CookieName}={originalCookie}");
        var firstRefreshResponse = await client.SendAsync(firstRefresh);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replay.Headers.Add("Cookie", $"{CookieName}={originalCookie}");
        var replayResponse = await client.SendAsync(replay);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sign_out_revokes_caller_refresh_token_and_clears_cookie()
    {
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "signout@example.com",
            displayName = "Signout Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        var refreshCookieValue = ExtractRefreshCookie(register);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        using var signOutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/sign-out");
        signOutRequest.Headers.Add("Cookie", $"{CookieName}={refreshCookieValue}");
        var signOut = await client.SendAsync(signOutRequest);
        signOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var clearingHeader = signOut.Headers.GetValues("Set-Cookie").Single(h => h.StartsWith($"{CookieName}=", StringComparison.Ordinal));
        clearingHeader.Should().MatchRegex("(?i)max-age=0");

        client.DefaultRequestHeaders.Authorization = null;
        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refresh.Headers.Add("Cookie", $"{CookieName}={refreshCookieValue}");
        var refreshResponse = await client.SendAsync(refresh);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string? ExtractRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            if (!header.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            {
                continue;
            }

            var nameValue = header.Split(';', 2)[0];
            var equalsIndex = nameValue.IndexOf('=');
            return nameValue[(equalsIndex + 1)..];
        }

        return null;
    }
}
