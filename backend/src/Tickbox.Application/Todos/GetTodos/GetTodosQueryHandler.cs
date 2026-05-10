using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Todos.GetTodos;

public sealed class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, IReadOnlyList<TodoListItem>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTodosQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TodoListItem>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        // REQ-TODO-3 AC4: order by DueDate ascending with nulls last, then CreatedAt descending.
        return await _db.Todos
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TodoListItem(t.Id, t.Title, t.Notes, t.DueDate, t.Status, t.CreatedAt, t.CompletedAt))
            .ToListAsync(cancellationToken);
    }
}
