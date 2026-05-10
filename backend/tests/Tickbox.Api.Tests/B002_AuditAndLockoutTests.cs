using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B002_AuditAndLockoutTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B002_AuditAndLockoutTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sign_in_locks_out_after_five_failures_in_window()
    {
        var client = _factory.CreateClient();
        const string email = "lockout@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Lockout Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var i = 1; i <= 5; i++)
        {
            var bad = await client.PostAsJsonAsync("/api/auth/sign-in", new
            {
                email,
                password = "wrong-password-attempt"
            });
            bad.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"attempt {i} should be a credential failure (not yet locked out)");
        }

        var locked = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email,
            password = "correct-horse-battery-staple"
        });
        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var attempts = await db.SignInAttempts.Where(a => a.Email == email).ToListAsync();
        attempts.Should().HaveCountGreaterThanOrEqualTo(6);

        var auditKinds = await db.SecurityAuditEvents
            .Where(e => e.UserId != null)
            .Select(e => e.Kind)
            .ToListAsync();

        auditKinds.Should().Contain(SecurityAuditKind.SignInFailed);
        auditKinds.Should().Contain(SecurityAuditKind.SignInLocked);
    }

    [Fact]
    public async Task Sign_in_with_null_password_hash_account_returns_401_generic()
    {
        var client = _factory.CreateClient();
        const string email = "oidc-only@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = "OIDC Only",
                PasswordHash = null,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email,
            password = "any-password-here"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
