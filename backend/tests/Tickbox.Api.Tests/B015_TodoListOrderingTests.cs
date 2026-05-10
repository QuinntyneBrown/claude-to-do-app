using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.GetTodos;

namespace Tickbox.Api.Tests;

public sealed class B015_TodoListOrderingTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B015_TodoListOrderingTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_todos_orders_by_due_date_ascending_nulls_last_then_created_at_descending()
    {
        var client = await SignedInClientAsync("ordering@example.com");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create in this order to ensure CreatedAt is monotonically increasing,
        // then the ordering rule should re-sort by (DueDate asc nulls last, CreatedAt desc).
        await CreateTodoAsync(client, "no due 1", null);
        await CreateTodoAsync(client, "due in 7", today.AddDays(7));
        await CreateTodoAsync(client, "due today", today);
        await CreateTodoAsync(client, "no due 2", null);
        await CreateTodoAsync(client, "due in 3", today.AddDays(3));

        var list = await client.GetFromJsonAsync<List<TodoListItem>>("/api/todos");
        list.Should().NotBeNull();
        list!.Select(t => t.Title).Should().ContainInOrder(
            "due today",   // earliest due date
            "due in 3",    // next due date
            "due in 7",    // last with a due date
            "no due 2",    // null due dates last; CreatedAt desc within nulls
            "no due 1");
    }

    private async Task CreateTodoAsync(HttpClient client, string title, DateOnly? dueDate)
    {
        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title,
            dueDate = dueDate?.ToString("yyyy-MM-dd")
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        // Slight delay so each row's CreatedAt is strictly monotonic at the resolution
        // the relational provider stores (the InMemory provider uses DateTimeOffset directly,
        // so any positive delta works).
        await Task.Delay(15);
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Ordering Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
