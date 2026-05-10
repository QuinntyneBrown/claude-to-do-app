using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Domain;

namespace Tickbox.Api.Tests;

public sealed class B013_ToggleTodoStatusTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public B013_ToggleTodoStatusTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Toggle_to_complete_sets_CompletedAt_and_writes_activity()
    {
        var client = await SignedInClientAsync("toggle-complete@example.com");
        var created = await CreateAsync(client, "becoming complete");

        var toggle = await PatchStatusAsync(client, created.Id, "Complete");
        toggle.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await toggle.Content.ReadFromJsonAsync<TodoDetail>();
        detail!.Status.Should().Be(TodoStatus.Complete);
        detail.CompletedAt.Should().NotBeNull();
        detail.Activity.Should().Contain(a => a.Kind == TodoActivityKind.MarkedComplete);
    }

    [Fact]
    public async Task Toggle_back_to_incomplete_clears_CompletedAt_and_removes_activity()
    {
        var client = await SignedInClientAsync("toggle-back@example.com");
        var created = await CreateAsync(client, "round-trip");

        await PatchStatusAsync(client, created.Id, "Complete");
        var back = await PatchStatusAsync(client, created.Id, "Incomplete");
        back.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await back.Content.ReadFromJsonAsync<TodoDetail>();
        detail!.Status.Should().Be(TodoStatus.Incomplete);
        detail.CompletedAt.Should().BeNull();
        detail.Activity.Should().NotContain(a => a.Kind == TodoActivityKind.MarkedComplete);
    }

    [Fact]
    public async Task Toggle_with_invalid_status_returns_400()
    {
        var client = await SignedInClientAsync("toggle-invalid@example.com");
        var created = await CreateAsync(client, "bad input");

        var response = await PatchStatusAsync(client, created.Id, "Archived");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<HttpResponseMessage> PatchStatusAsync(HttpClient client, Guid id, string status)
    {
        return await client.PatchAsJsonAsync($"/api/todos/{id}/status", new { status });
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
            displayName = "Toggle Tester",
            password = "correct-horse-battery-staple"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = (await register.Content.ReadFromJsonAsync<RegisterUserResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
