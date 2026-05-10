using FluentValidation;

namespace Tickbox.Application.Account.EmailChange;

public sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
