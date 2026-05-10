using MediatR;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Domain;

namespace Tickbox.Application.Todos.ToggleTodoStatus;

public sealed record ToggleTodoStatusCommand(Guid Id, TodoStatus Status) : IRequest<TodoDetail>;
