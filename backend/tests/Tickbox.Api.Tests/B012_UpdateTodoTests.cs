using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B012_UpdateTodoTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B012_UpdateTodoTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_todo_persists_new_fields()
    {
        var client = await SignedInClientAsync("update-fields@example.com");
        var created = await CreateAsync(client, "original title", "original notes", null);

        var newDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var update = await client.PutAsJsonAsync($"/api/todos/{created.Id}", new
        {
            title = "updated title",
            notes = "updated notes",
            dueDate = newDueDate.ToString("yyyy-MM-dd")
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await update.Content.ReadFromJsonAsync<TodoDetail>();
        detail!.Title.Should().Be("updated title");
        detail.Notes.Should().Be("updated notes");
        detail.DueDate.Should().Be(newDueDate);
    }

    [Fact]
    public async Task Update_todo_does_not_change_status()
    {
        var client = await SignedInClientAsync("update-keep-status@example.com");
        var created = await CreateAsync(client, "keep status", null, null);

        // Manually flip the persisted Status so we can prove Update preserves it.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var todo = await db.Todos.SingleAsync(t => t.Id == created.Id);
            todo.Status = TodoStatus.Complete;
            todo.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var update = await client.PutAsJsonAsync($"/api/todos/{created.Id}", new
        {
            title = "Different title",
            notes = (string?)null,
            dueDate = (string?)null
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = (await update.Content.ReadFromJsonAsync<TodoDetail>())!;
        detail.Title.Should().Be("Different title");
        detail.Status.Should().Be(TodoStatus.Complete, "Update must not flip status — that's the toggle command's job");
        detail.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_other_users_todo_returns_404()
    {
        var ownerClient = await SignedInClientAsync("update-owner@example.com");
        var created = await CreateAsync(ownerClient, "owner only", null, null);

        var intruder = await SignedInClientAsync("update-intruder@example.com");
        var update = await intruder.PutAsJsonAsync($"/api/todos/{created.Id}", new
        {
            title = "hacked",
            notes = (string?)null,
            dueDate = (string?)null
        });
        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<CreateTodoResult> CreateAsync(HttpClient client, string title, string? notes, DateOnly? dueDate)
    {
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title,
            notes,
            dueDate = dueDate?.ToString("yyyy-MM-dd")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreateTodoResult>())!;
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Update Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
