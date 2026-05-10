namespace Tickbox.Api.Controllers;

public sealed record CreateTodoRequest(string Title, string? Notes, DateOnly? DueDate);
