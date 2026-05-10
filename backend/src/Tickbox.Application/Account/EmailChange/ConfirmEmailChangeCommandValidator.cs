using FluentValidation;

namespace Tickbox.Application.Account.EmailChange;

public sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
