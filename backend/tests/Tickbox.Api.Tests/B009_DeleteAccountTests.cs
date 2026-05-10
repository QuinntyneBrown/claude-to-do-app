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

public sealed class B009_DeleteAccountTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B009_DeleteAccountTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_account_removes_user_and_all_owned_rows()
    {
        var client = _factory.CreateClient();
        const string email = "delete-cascade@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Cascade Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var todoResponse = await client.PostAsJsonAsync("/api/todos", new { title = "doomed todo" });
        todoResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var delete = await client.DeleteAsync("/api/account");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userExists = await db.Users.AnyAsync(u => u.Id == session.UserId);
        userExists.Should().BeFalse();

        var ownedTodos = await db.Todos.AnyAsync(t => t.UserId == session.UserId);
        ownedTodos.Should().BeFalse();

        var refreshTokens = await db.RefreshTokens.AnyAsync(t => t.UserId == session.UserId);
        refreshTokens.Should().BeFalse();

        var userRoles = await db.UserRoles.AnyAsync(ur => ur.UserId == session.UserId);
        userRoles.Should().BeFalse();

        var auditKinds = await db.SecurityAuditEvents
            .Where(e => e.UserId == session.UserId)
            .Select(e => e.Kind)
            .ToListAsync();
        auditKinds.Should().Contain(SecurityAuditKind.AccountDeleted);
    }

    [Fact]
    public async Task Delete_account_invalidates_existing_access_jwt()
    {
        var client = _factory.CreateClient();
        const string email = "delete-jwt@example.com";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "JWT Tester",
            password = "correct-horse-battery-staple"
        });
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        await client.DeleteAsync("/api/account");

        var afterDelete = await client.GetAsync("/api/account/me");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the account is gone so the per-user lookup must fail with 404 (the access JWT signature is still valid but useless)");
    }
}
