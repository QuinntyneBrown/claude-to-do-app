using FluentValidation;

namespace Tickbox.Application.Auth.PasswordReset;

public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(Common.PasswordPolicy.MinLength)
            .MaximumLength(Common.PasswordPolicy.MaxLength);
    }
}
