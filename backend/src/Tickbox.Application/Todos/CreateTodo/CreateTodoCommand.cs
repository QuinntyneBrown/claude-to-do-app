using MediatR;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed record CreateTodoCommand(string Title) : IRequest<CreateTodoResult>;
