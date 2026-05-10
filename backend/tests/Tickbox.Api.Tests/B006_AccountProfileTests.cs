using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Account;
using Tickbox.Application.Auth.RegisterUser;

namespace Tickbox.Api.Tests;

public sealed class B006_AccountProfileTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B006_AccountProfileTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_me_returns_authenticated_users_profile()
    {
        var client = await SignedInClientAsync("get-me@example.com", "Get Me Tester");

        var response = await client.GetAsync("/api/account/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<MyProfile>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("get-me@example.com");
        profile.DisplayName.Should().Be("Get Me Tester");
    }

    [Fact]
    public async Task Update_display_name_persists_and_returns_new_profile()
    {
        var client = await SignedInClientAsync("update-name@example.com", "Old Name");

        var update = await client.PutAsJsonAsync("/api/account/display-name", new { displayName = "New Name" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await update.Content.ReadFromJsonAsync<MyProfile>();
        profile!.DisplayName.Should().Be("New Name");

        var fetch = await client.GetFromJsonAsync<MyProfile>("/api/account/me");
        fetch!.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public async Task Update_display_name_rejects_blank_or_too_long()
    {
        var client = await SignedInClientAsync("name-validation@example.com", "Validation Tester");

        var blank = await client.PutAsJsonAsync("/api/account/display-name", new { displayName = "" });
        blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tooLong = await client.PutAsJsonAsync("/api/account/display-name", new { displayName = new string('x', 101) });
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> SignedInClientAsync(string email, string displayName)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName,
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
