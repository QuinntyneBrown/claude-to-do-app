using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B014_DeleteTodoTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B014_DeleteTodoTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_todo_removes_row_and_cascades_activity()
    {
        var client = await SignedInClientAsync("delete-cascade@example.com");
        var created = await CreateAsync(client, "doomed");

        // Toggle status so there's a MarkedComplete activity row alongside the Created row.
        await client.PatchAsJsonAsync($"/api/todos/{created.Id}/status", new { status = "Complete" });

        var response = await client.DeleteAsync($"/api/todos/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Todos.AnyAsync(t => t.Id == created.Id)).Should().BeFalse();
        (await db.TodoActivityEntries.AnyAsync(a => a.TodoId == created.Id)).Should().BeFalse(
            "deleting a todo cascades its activity entries");
    }

    [Fact]
    public async Task Delete_other_users_todo_returns_404()
    {
        var owner = await SignedInClientAsync("delete-owner@example.com");
        var created = await CreateAsync(owner, "owner only");

        var intruder = await SignedInClientAsync("delete-intruder@example.com");
        var response = await intruder.DeleteAsync($"/api/todos/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Todos.AnyAsync(t => t.Id == created.Id)).Should().BeTrue("the owner's todo must still exist");
    }

    private static async Task<CreateTodoResult> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/todos", new { title });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreateTodoResult>())!;
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Delete Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
