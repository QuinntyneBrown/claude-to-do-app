using Tickbox.Domain;

namespace Tickbox.Application.Todos.GetTodoById;

public sealed record TodoActivityItem(TodoActivityKind Kind, DateTimeOffset OccurredAt);
