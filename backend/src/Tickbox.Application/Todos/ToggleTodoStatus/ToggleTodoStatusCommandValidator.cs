using FluentValidation;
using Tickbox.Domain;

namespace Tickbox.Application.Todos.ToggleTodoStatus;

public sealed class ToggleTodoStatusCommandValidator : AbstractValidator<ToggleTodoStatusCommand>
{
    public ToggleTodoStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be Incomplete or Complete.");
    }
}
