using FluentValidation;

namespace Tickbox.Application.Auth.SignInUser;

public sealed class SignInUserCommandValidator : AbstractValidator<SignInUserCommand>
{
    public SignInUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
