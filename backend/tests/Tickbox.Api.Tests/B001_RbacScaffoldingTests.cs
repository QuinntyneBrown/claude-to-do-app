using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;

namespace Tickbox.Api.Tests;

public sealed class B001_RbacScaffoldingTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B001_RbacScaffoldingTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_creates_user_with_User_role_and_token_carries_role_claim()
    {
        var client = _factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "rbac-user@example.com",
            displayName = "RBAC Tester",
            password = "correct-horse-battery-staple"
        });

        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await register.Content.ReadFromJsonAsync<RegisterUserResult>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken);
        jwt.Claims.Should().Contain(c =>
            (c.Type == "role" || c.Type.EndsWith("/role", StringComparison.Ordinal) || c.Type == System.Security.Claims.ClaimTypes.Role)
            && c.Value == "User");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        var todos = await client.GetAsync("/api/todos");
        todos.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
