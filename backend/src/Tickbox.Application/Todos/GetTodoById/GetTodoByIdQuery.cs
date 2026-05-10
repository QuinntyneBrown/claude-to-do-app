using MediatR;

namespace Tickbox.Application.Todos.GetTodoById;

public sealed record GetTodoByIdQuery(Guid Id) : IRequest<TodoDetail>;
