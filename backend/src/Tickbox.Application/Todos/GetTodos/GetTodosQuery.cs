using MediatR;

namespace Tickbox.Application.Todos.GetTodos;

public sealed record GetTodosQuery() : IRequest<IReadOnlyList<TodoListItem>>;
