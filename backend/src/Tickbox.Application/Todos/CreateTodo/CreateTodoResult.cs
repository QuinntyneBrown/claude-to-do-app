using Tickbox.Domain;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed record CreateTodoResult(Guid Id, string Title, TodoStatus Status, DateTimeOffset CreatedAt);
