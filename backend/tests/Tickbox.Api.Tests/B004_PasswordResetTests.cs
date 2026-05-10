using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B004_PasswordResetTests : IClassFixture<TickboxApiFactory>
{
    private const string CookieName = "tickbox.refresh";

    private readonly TickboxApiFactory _factory;

    public B004_PasswordResetTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_password_reset_returns_202_for_unknown_and_known_emails()
    {
        var client = _factory.CreateClient();
        const string knownEmail = "reset-known@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = knownEmail,
            displayName = "Reset Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var unknownResponse = await client.PostAsJsonAsync("/api/auth/password-reset/request", new
        {
            email = "unknown-account-2026@example.com"
        });
        unknownResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var knownResponse = await client.PostAsJsonAsync("/api/auth/password-reset/request", new
        {
            email = knownEmail
        });
        knownResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var knownUser = await db.Users.SingleAsync(u => u.Email == knownEmail);

        var auditKinds = await db.SecurityAuditEvents
            .Where(e => e.UserId == knownUser.Id)
            .Select(e => e.Kind)
            .ToListAsync();
        auditKinds.Should().Contain(SecurityAuditKind.PasswordResetRequested);

        var knownResetTokens = await db.PasswordResetTokens.Where(t => t.UserId == knownUser.Id).ToListAsync();
        knownResetTokens.Should().HaveCount(1, "exactly one reset row for the known user");

        var unknownUsers = await db.Users.Where(u => u.Email == "unknown-account-2026@example.com").AnyAsync();
        unknownUsers.Should().BeFalse("the unknown email must not have a user row");
    }

    [Fact]
    public async Task Complete_password_reset_with_valid_token_signs_user_in_and_revokes_existing_sessions()
    {
        var client = _factory.CreateClient();
        const string email = "reset-complete@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Complete Reset Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var registered = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        var originalRefreshCookie = ExtractRefreshCookie(register);
        originalRefreshCookie.Should().NotBeNull();

        var (resetPlaintext, _) = await IssueResetTokenViaServiceAsync(email);

        var complete = await client.PostAsJsonAsync("/api/auth/password-reset/complete", new
        {
            token = resetPlaintext,
            newPassword = "brand-new-passphrase-123"
        });
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        var completeResult = await complete.Content.ReadFromJsonAsync<RegisterUserResult>();
        completeResult.Should().NotBeNull();
        completeResult!.UserId.Should().Be(registered.UserId);
        completeResult.AccessToken.Should().NotBeNullOrWhiteSpace();

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replay.Headers.Add("Cookie", $"{CookieName}={originalRefreshCookie}");
        var replayResponse = await client.SendAsync(replay);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditKinds = await db.SecurityAuditEvents.Select(e => e.Kind).ToListAsync();
        auditKinds.Should().Contain(SecurityAuditKind.PasswordResetUsed);
    }

    [Fact]
    public async Task Complete_password_reset_with_expired_token_returns_400()
    {
        var client = _factory.CreateClient();
        const string email = "reset-expired@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Expired Reset Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var (resetPlaintext, persisted) = await IssueResetTokenViaServiceAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var token = await db.PasswordResetTokens.SingleAsync(t => t.Id == persisted.Id);
            token.ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        var complete = await client.PostAsJsonAsync("/api/auth/password-reset/complete", new
        {
            token = resetPlaintext,
            newPassword = "another-strong-passphrase"
        });

        complete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(string Plaintext, PasswordResetToken Persisted)> IssueResetTokenViaServiceAsync(string email)
    {
        // Drive a request through the API so the handler creates the row;
        // then read the most-recently-created row's plaintext via the test seam.
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/password-reset/request", new { email });
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var captured = TestEmailService.LastResetToken
            ?? throw new InvalidOperationException("Test email service did not capture a reset token");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.PasswordResetTokens.OrderByDescending(t => t.Id).FirstAsync();

        return (captured, persisted);
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
            return nameValue[(nameValue.IndexOf('=') + 1)..];
        }

        return null;
    }
}
