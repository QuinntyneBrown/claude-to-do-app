using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Application.Todos.CreateTodo;
using Tickbox.Application.Todos.DeleteTodo;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Application.Todos.GetTodos;
using Tickbox.Application.Todos.ToggleTodoStatus;
using Tickbox.Application.Todos.UpdateTodo;

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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoDetail>> Update(Guid id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new UpdateTodoCommand(id, request.Title, request.Notes, request.DueDate), cancellationToken);
        return Ok(detail);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TodoDetail>> ToggleStatus(Guid id, [FromBody] ToggleTodoStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Tickbox.Domain.TodoStatus>(request.Status, ignoreCase: false, out var status)
            || !Enum.IsDefined(typeof(Tickbox.Domain.TodoStatus), status))
        {
            throw new Tickbox.Application.Common.ValidationFailureException("status", "Status must be Incomplete or Complete.");
        }

        var detail = await _mediator.Send(new ToggleTodoStatusCommand(id, status), cancellationToken);
        return Ok(detail);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTodoCommand(id), cancellationToken);
        return NoContent();
    }
}
