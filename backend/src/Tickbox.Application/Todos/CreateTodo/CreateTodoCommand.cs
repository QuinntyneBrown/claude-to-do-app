using MediatR;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed record CreateTodoCommand(string Title, string? Notes, DateOnly? DueDate) : IRequest<CreateTodoResult>;
