using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Todos.GetTodoById;

public sealed class GetTodoByIdQueryHandler : IRequestHandler<GetTodoByIdQuery, TodoDetail>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTodoByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TodoDetail> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // Same code path for "not found" and "not owned" — never reveal which.
        var todo = await _db.Todos
            .SingleOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Todo not found.");

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
