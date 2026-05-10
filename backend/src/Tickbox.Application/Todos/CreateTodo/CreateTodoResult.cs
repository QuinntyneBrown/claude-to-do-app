using Tickbox.Domain;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed record CreateTodoResult(
    Guid Id,
    string Title,
    string? Notes,
    DateOnly? DueDate,
    TodoStatus Status,
    DateTimeOffset CreatedAt);
