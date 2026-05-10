using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Account;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B007_EmailChangeTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B007_EmailChangeTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_email_change_persists_pending_and_keeps_login_email()
    {
        var client = await SignedInClientAsync("change-keep@example.com", "Original");
        TestEmailService.Reset();

        var response = await client.PostAsJsonAsync("/api/account/email-change/request", new
        {
            newEmail = "change-keep-new@example.com"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<MyProfile>();
        profile!.Email.Should().Be("change-keep@example.com", "the sign-in email must not change yet");
        profile.PendingEmail.Should().Be("change-keep-new@example.com");

        TestEmailService.LastEmailChangeAddress.Should().Be("change-keep-new@example.com");
        TestEmailService.LastEmailChangeToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Confirm_email_change_with_valid_token_swaps_login_email()
    {
        var client = await SignedInClientAsync("change-confirm@example.com", "Confirm Tester");
        TestEmailService.Reset();

        await client.PostAsJsonAsync("/api/account/email-change/request", new
        {
            newEmail = "change-confirm-new@example.com"
        });
        var token = TestEmailService.LastEmailChangeToken!;

        var confirm = await client.PostAsJsonAsync("/api/account/email-change/confirm", new { token });
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await confirm.Content.ReadFromJsonAsync<MyProfile>();
        profile!.Email.Should().Be("change-confirm-new@example.com");
        profile.PendingEmail.Should().BeNull();
    }

    [Fact]
    public async Task Confirm_email_change_with_expired_token_returns_400()
    {
        var client = await SignedInClientAsync("change-expired@example.com", "Expired Tester");
        TestEmailService.Reset();

        await client.PostAsJsonAsync("/api/account/email-change/request", new
        {
            newEmail = "change-expired-new@example.com"
        });
        var token = TestEmailService.LastEmailChangeToken!;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == "change-expired@example.com");
            user.PendingEmailExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        var confirm = await client.PostAsJsonAsync("/api/account/email-change/confirm", new { token });
        confirm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_email_change_clears_pending_state()
    {
        var client = await SignedInClientAsync("change-cancel@example.com", "Cancel Tester");
        TestEmailService.Reset();

        await client.PostAsJsonAsync("/api/account/email-change/request", new
        {
            newEmail = "change-cancel-new@example.com"
        });

        var cancel = await client.DeleteAsync("/api/account/email-change");
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await cancel.Content.ReadFromJsonAsync<MyProfile>();
        profile!.Email.Should().Be("change-cancel@example.com");
        profile.PendingEmail.Should().BeNull();
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
