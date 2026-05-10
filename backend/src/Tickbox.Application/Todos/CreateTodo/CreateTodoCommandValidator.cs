using FluentValidation;

namespace Tickbox.Application.Todos.CreateTodo;

public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(TimeProvider clock)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);

        RuleFor(x => x.DueDate)
            .Must(d => d is null || d.Value >= today)
            .WithMessage("Due date must be today or later.");
    }
}
