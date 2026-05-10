using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.Oidc;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B005_OidcSignInTests : IClassFixture<OidcEnabledFactory>
{
    private readonly OidcEnabledFactory _factory;

    public B005_OidcSignInTests(OidcEnabledFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Begin_oidc_returns_authorization_url_and_persists_state()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/oidc/authorize");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<BeginOidcSignInResult>();
        payload.Should().NotBeNull();
        payload!.AuthorizationUrl.Should().StartWith("https://idp.test/");
        payload.AuthorizationUrl.Should().Contain("client_id=tickbox-test");
        payload.AuthorizationUrl.Should().Contain("code_challenge=");
        payload.AuthorizationUrl.Should().Contain("code_challenge_method=S256");
        payload.AuthorizationUrl.Should().Contain($"state={payload.State}");
        payload.State.Should().NotBeNullOrEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.OidcAuthorizationRequests.SingleOrDefaultAsync(r => r.State == payload.State);
        stored.Should().NotBeNull();
        stored!.CodeVerifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Complete_oidc_first_time_provisions_user_with_role_and_no_password()
    {
        var client = _factory.CreateClient();
        TestOidcClient.NextUser = new OidcUserInfo("idp-sub-001", "first-time@example.com", "First Time");

        var begin = await client.GetAsync("/api/auth/oidc/authorize");
        var beginPayload = (await begin.Content.ReadFromJsonAsync<BeginOidcSignInResult>())!;

        var callback = await client.PostAsJsonAsync("/api/auth/oidc/callback", new
        {
            code = "fake-authorization-code",
            state = beginPayload.State
        });
        callback.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await callback.Content.ReadFromJsonAsync<RegisterUserResult>();
        session.Should().NotBeNull();
        session!.AccessToken.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "first-time@example.com");
        user.PasswordHash.Should().BeNull("OIDC-only accounts have no local password");

        var roleAttached = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == KnownRoles.UserRoleId);
        roleAttached.Should().BeTrue();
    }

    [Fact]
    public async Task Complete_oidc_returning_user_signs_in_without_provisioning()
    {
        var client = _factory.CreateClient();
        TestOidcClient.NextUser = new OidcUserInfo("idp-sub-002", "returning@example.com", "Returning User");

        var firstBegin = await client.GetAsync("/api/auth/oidc/authorize");
        var firstPayload = (await firstBegin.Content.ReadFromJsonAsync<BeginOidcSignInResult>())!;
        var firstCallback = await client.PostAsJsonAsync("/api/auth/oidc/callback", new
        {
            code = "fake-1",
            state = firstPayload.State
        });
        firstCallback.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondBegin = await client.GetAsync("/api/auth/oidc/authorize");
        var secondPayload = (await secondBegin.Content.ReadFromJsonAsync<BeginOidcSignInResult>())!;
        var secondCallback = await client.PostAsJsonAsync("/api/auth/oidc/callback", new
        {
            code = "fake-2",
            state = secondPayload.State
        });
        secondCallback.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.Where(u => u.Email == "returning@example.com").ToListAsync();
        users.Should().HaveCount(1, "the second OIDC sign-in must reuse the existing user");
    }
}

public sealed class B005_OidcDisabledTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B005_OidcDisabledTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Oidc_endpoints_return_404_when_disabled()
    {
        var client = _factory.CreateClient();

        var begin = await client.GetAsync("/api/auth/oidc/authorize");
        begin.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var callback = await client.PostAsJsonAsync("/api/auth/oidc/callback", new
        {
            code = "anything",
            state = "anything"
        });
        callback.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
