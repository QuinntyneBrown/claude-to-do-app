using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B008_ChangePasswordTests : IClassFixture<TickboxApiFactory>
{
    private const string CookieName = "tickbox.refresh";

    private readonly TickboxApiFactory _factory;

    public B008_ChangePasswordTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Change_password_with_correct_current_persists_new_hash()
    {
        var client = _factory.CreateClient();
        var register = await RegisterAsync(client, "change-correct@example.com", "Original");
        var (accessToken, cookie) = (register.Session.AccessToken, register.RefreshCookie!);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var change = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = "correct-horse-battery-staple",
                newPassword = "brand-new-passphrase-123"
            })
        };
        change.Headers.Add("Cookie", $"{CookieName}={cookie}");
        var response = await client.SendAsync(change);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        var signInOld = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "change-correct@example.com",
            password = "correct-horse-battery-staple"
        });
        signInOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var signInNew = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "change-correct@example.com",
            password = "brand-new-passphrase-123"
        });
        signInNew.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Change_password_with_wrong_current_returns_400_and_audits()
    {
        var client = _factory.CreateClient();
        var register = await RegisterAsync(client, "change-wrong@example.com", "Wrong-Current Tester");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", register.Session.AccessToken);
        using var change = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = "totally-wrong-passphrase",
                newPassword = "brand-new-passphrase-123"
            })
        };
        change.Headers.Add("Cookie", $"{CookieName}={register.RefreshCookie}");
        var response = await client.SendAsync(change);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "change-wrong@example.com");
        var auditKinds = await db.SecurityAuditEvents
            .Where(e => e.UserId == user.Id)
            .Select(e => e.Kind)
            .ToListAsync();
        auditKinds.Should().Contain(SecurityAuditKind.SignInFailed);
    }

    [Fact]
    public async Task Change_password_revokes_other_sessions_only()
    {
        var client = _factory.CreateClient();
        var first = await RegisterAsync(client, "change-multi@example.com", "Multi-Session Tester");

        var secondSignIn = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "change-multi@example.com",
            password = "correct-horse-battery-staple"
        });
        secondSignIn.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondCookie = ExtractRefreshCookie(secondSignIn);
        secondCookie.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.Session.AccessToken);
        using var change = new HttpRequestMessage(HttpMethod.Put, "/api/account/password")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = "correct-horse-battery-staple",
                newPassword = "brand-new-passphrase-456"
            })
        };
        change.Headers.Add("Cookie", $"{CookieName}={first.RefreshCookie}");
        var response = await client.SendAsync(change);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;

        using var refreshCaller = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshCaller.Headers.Add("Cookie", $"{CookieName}={first.RefreshCookie}");
        var refreshCallerResponse = await client.SendAsync(refreshCaller);
        refreshCallerResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the caller's session must remain alive");

        using var refreshOther = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshOther.Headers.Add("Cookie", $"{CookieName}={secondCookie}");
        var refreshOtherResponse = await client.SendAsync(refreshOther);
        refreshOtherResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the other session must be revoked");
    }

    private static async Task<(RegisterUserResult Session, string? RefreshCookie)> RegisterAsync(HttpClient client, string email, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName,
            password = "correct-horse-battery-staple"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await response.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        return (session, ExtractRefreshCookie(response));
    }

    private static string? ExtractRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;
        foreach (var header in values)
        {
            if (!header.StartsWith($"{CookieName}=", StringComparison.Ordinal)) continue;
            var nv = header.Split(';', 2)[0];
            return nv[(nv.IndexOf('=') + 1)..];
        }
        return null;
    }
}
