using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Domain;

namespace Tickbox.Api.Tests;

public sealed class B011_GetTodoByIdTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B011_GetTodoByIdTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_todo_by_id_returns_full_detail_including_activity()
    {
        var client = await SignedInClientAsync("get-by-id@example.com");

        var create = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "Detail target",
            notes = "Some background notes",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5).ToString("yyyy-MM-dd")
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<CreateTodoResult>())!;

        var response = await client.GetAsync($"/api/todos/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<TodoDetail>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(created.Id);
        detail.Title.Should().Be("Detail target");
        detail.Notes.Should().Be("Some background notes");
        detail.Status.Should().Be(TodoStatus.Incomplete);
        detail.Activity.Should().Contain(a => a.Kind == TodoActivityKind.Created);
    }

    [Fact]
    public async Task Get_todo_owned_by_other_user_returns_404()
    {
        var ownerClient = await SignedInClientAsync("owner@example.com");
        var create = await ownerClient.PostAsJsonAsync("/api/todos", new { title = "Owner only" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<CreateTodoResult>())!;

        var otherClient = await SignedInClientAsync("intruder@example.com");
        var response = await otherClient.GetAsync($"/api/todos/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> SignedInClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            displayName = "Detail Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
