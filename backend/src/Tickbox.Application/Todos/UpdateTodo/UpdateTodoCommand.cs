using MediatR;
using Tickbox.Application.Todos.GetTodoById;

namespace Tickbox.Application.Todos.UpdateTodo;

public sealed record UpdateTodoCommand(Guid Id, string Title, string? Notes, DateOnly? DueDate) : IRequest<TodoDetail>;
