using MediatR;
using Tickbox.Application.Common;
using Tickbox.Domain;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed class CreateTodoCommandHandler : IRequestHandler<CreateTodoCommand, CreateTodoResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public CreateTodoCommandHandler(IAppDbContext db, ICurrentUserService currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CreateTodoResult> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            Title = request.Title.Trim(),
            Status = TodoStatus.Incomplete,
            CreatedAt = _clock.GetUtcNow()
        };

        _db.Todos.Add(todo);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateTodoResult(todo.Id, todo.Title, todo.Status, todo.CreatedAt);
    }
}
