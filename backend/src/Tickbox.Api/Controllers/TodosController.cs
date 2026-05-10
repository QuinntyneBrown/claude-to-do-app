using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.GetTodoById;
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoDetail>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetTodoByIdQuery(id), cancellationToken);
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<CreateTodoResult>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTodoCommand(request.Title, request.Notes, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
