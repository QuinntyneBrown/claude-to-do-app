using Tickbox.Domain;

namespace Tickbox.Application.Todos.GetTodos;

public sealed record TodoListItem(
    Guid Id,
    string Title,
    string? Notes,
    DateOnly? DueDate,
    TodoStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
