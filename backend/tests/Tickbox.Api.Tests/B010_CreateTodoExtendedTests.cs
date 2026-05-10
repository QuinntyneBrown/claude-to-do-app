using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Domain;
using Tickbox.Infrastructure.Persistence;

namespace Tickbox.Api.Tests;

public sealed class B010_CreateTodoExtendedTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B010_CreateTodoExtendedTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_todo_with_notes_and_due_date_persists_and_writes_Created_activity()
    {
        var client = await SignedInClientAsync("create-extended@example.com");
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "Draft launch announcement",
            notes = "Outline the v1 announcement: target message, channels, owner, ship-by date.",
            dueDate = dueDate.ToString("yyyy-MM-dd")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateTodoResult>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("Draft launch announcement");
        created.Notes.Should().Be("Outline the v1 announcement: target message, channels, owner, ship-by date.");
        created.DueDate.Should().Be(dueDate);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activity = await db.TodoActivityEntries
            .Where(a => a.TodoId == created.Id)
            .Select(a => a.Kind)
            .ToListAsync();
        activity.Should().Contain(TodoActivityKind.Created);
    }

    [Fact]
    public async Task Create_todo_rejects_notes_over_limit_and_past_due_date()
    {
        var client = await SignedInClientAsync("create-validation@example.com");

        var oversizedNotes = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "Has too many notes",
            notes = new string('x', 2001)
        });
        oversizedNotes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var pastDueDate = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "Already overdue",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd")
        });
        pastDueDate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Todo Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
