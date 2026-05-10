using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodos;

namespace Tickbox.Api.Controllers;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/todos")]
public sealed class TodosController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoListItem>>> GetAll(CancellationToken cancellationToken)
    {
        var todos = await _mediator.Send(new GetTodosQuery(), cancellationToken);
        return Ok(todos);
    }

    [HttpPost]
    public async Task<ActionResult<CreateTodoResult>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTodoCommand(request.Title, request.Notes, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }
}
