using Tickbox.Domain;

namespace Tickbox.Application.Todos.GetTodos;

public sealed record TodoListItem(Guid Id, string Title, TodoStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
