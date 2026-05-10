using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Application.Todos.GetTodoById;

namespace Tickbox.Application.Todos.UpdateTodo;

public sealed class UpdateTodoCommandHandler : IRequestHandler<UpdateTodoCommand, TodoDetail>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTodoCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TodoDetail> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var todo = await _db.Todos
            .SingleOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Todo not found.");

        todo.Title = request.Title.Trim();
        todo.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        todo.DueDate = request.DueDate;
        // Status is intentionally NOT touched — it is owned by ToggleTodoStatusCommand.

        await _db.SaveChangesAsync(cancellationToken);

        var activity = await _db.TodoActivityEntries
            .Where(a => a.TodoId == todo.Id)
            .OrderBy(a => a.OccurredAt)
            .Select(a => new TodoActivityItem(a.Kind, a.OccurredAt))
            .ToListAsync(cancellationToken);

        return new TodoDetail(
            todo.Id,
            todo.Title,
            todo.Notes,
            todo.DueDate,
            todo.Status,
            todo.CreatedAt,
            todo.CompletedAt,
            activity);
    }
}
