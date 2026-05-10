using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;
using Tickbox.Application.Todos.GetTodoById;
using Tickbox.Domain;

namespace Tickbox.Application.Todos.ToggleTodoStatus;

public sealed class ToggleTodoStatusCommandHandler : IRequestHandler<ToggleTodoStatusCommand, TodoDetail>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public ToggleTodoStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<TodoDetail> Handle(ToggleTodoStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var now = _clock.GetUtcNow();

        var todo = await _db.Todos
            .SingleOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Todo not found.");

        if (todo.Status != request.Status)
        {
            if (request.Status == TodoStatus.Complete)
            {
                todo.Status = TodoStatus.Complete;
                todo.CompletedAt = now;
                _db.TodoActivityEntries.Add(new TodoActivityEntry
                {
                    Id = Guid.NewGuid(),
                    TodoId = todo.Id,
                    Kind = TodoActivityKind.MarkedComplete,
                    OccurredAt = now
                });
            }
            else
            {
                todo.Status = TodoStatus.Incomplete;
                todo.CompletedAt = null;

                var latestMarkedComplete = await _db.TodoActivityEntries
                    .Where(a => a.TodoId == todo.Id && a.Kind == TodoActivityKind.MarkedComplete)
                    .OrderByDescending(a => a.OccurredAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (latestMarkedComplete is not null)
                {
                    _db.TodoActivityEntries.Remove(latestMarkedComplete);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

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
