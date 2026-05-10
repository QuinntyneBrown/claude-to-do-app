using Tickbox.Domain;

namespace Tickbox.Application.Todos.GetTodoById;

public sealed record TodoDetail(
    Guid Id,
    string Title,
    string? Notes,
    DateOnly? DueDate,
    TodoStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<TodoActivityItem> Activity);
