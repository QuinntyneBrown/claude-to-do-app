using FluentValidation;
using Tickbox.Application.Common;

namespace Tickbox.Application.Auth.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(PasswordPolicy.MinLength)
            .MaximumLength(PasswordPolicy.MaxLength);
    }
}
