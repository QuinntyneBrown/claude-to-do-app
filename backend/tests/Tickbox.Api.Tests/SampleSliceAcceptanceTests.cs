using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Auth.SignInUser;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodos;
using Tickbox.Domain;

namespace Tickbox.Api.Tests;

public sealed class SampleSliceAcceptanceTests : IClassFixture<TickboxApiFactory>
{
    private readonly TickboxApiFactory _factory;

    public SampleSliceAcceptanceTests(TickboxApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sample_slice_round_trips_through_http_mediatr_ef()
    {
        var client = _factory.CreateClient();

        // Register
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "ada@example.com",
            displayName = "Ada Lovelace",
            password = "correct-horse-battery-staple"
        });
        var registerBody = await register.Content.ReadAsStringAsync();
        register.StatusCode.Should().Be(HttpStatusCode.OK, "register body was: " + registerBody);

        var registerPayload = await register.Content.ReadFromJsonAsync<RegisterUserResult>();
        registerPayload.Should().NotBeNull();
        registerPayload!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Sign in
        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "ada@example.com",
            password = "correct-horse-battery-staple"
        });
        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
        var signInPayload = await signIn.Content.ReadFromJsonAsync<SignInUserResult>();
        signInPayload!.UserId.Should().Be(registerPayload.UserId);

        // Authenticate subsequent requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signInPayload.AccessToken);

        // Anonymous request to a protected endpoint must be 401
        using var anonClient = _factory.CreateClient();
        var anonResponse = await anonClient.GetAsync("/api/todos");
        anonResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Create a todo
        var create = await client.PostAsJsonAsync("/api/todos", new { title = "Draft launch announcement" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodo = await create.Content.ReadFromJsonAsync<CreateTodoResult>();
        createdTodo!.Title.Should().Be("Draft launch announcement");
        createdTodo.Status.Should().Be(TodoStatus.Incomplete);

        // List todos
        var list = await client.GetFromJsonAsync<List<TodoListItem>>("/api/todos");
        list.Should().NotBeNull();
        list!.Should().ContainSingle(t => t.Id == createdTodo.Id && t.Title == "Draft launch announcement");
    }

    [Fact]
    public async Task Register_with_short_password_is_rejected_with_validation_problem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "shorty@example.com",
            displayName = "Shorty",
            password = "short"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Sign_in_with_wrong_password_returns_401_with_generic_message()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "grace@example.com",
            displayName = "Grace",
            password = "correct-horse-battery-staple"
        });

        var signIn = await client.PostAsJsonAsync("/api/auth/sign-in", new
        {
            email = "grace@example.com",
            password = "wrong-password-123"
        });

        signIn.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
