using MediatR;

namespace Tickbox.Application.Todos.DeleteTodo;

public sealed record DeleteTodoCommand(Guid Id) : IRequest<Unit>;
