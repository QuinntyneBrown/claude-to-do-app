namespace Tickbox.Api.Controllers;

public sealed record UpdateTodoRequest(string Title, string? Notes, DateOnly? DueDate);
