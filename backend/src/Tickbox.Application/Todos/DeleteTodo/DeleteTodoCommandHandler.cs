using MediatR;
using Microsoft.EntityFrameworkCore;
using Tickbox.Application.Common;

namespace Tickbox.Application.Todos.DeleteTodo;

public sealed class DeleteTodoCommandHandler : IRequestHandler<DeleteTodoCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteTodoCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var todo = await _db.Todos
            .SingleOrDefaultAsync(t => t.Id == request.Id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Todo not found.");

        // Mirror the relational cascade on InMemory: explicitly remove activity rows.
        var activity = await _db.TodoActivityEntries
            .Where(a => a.TodoId == todo.Id)
            .ToListAsync(cancellationToken);
        _db.TodoActivityEntries.RemoveRange(activity);

        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
