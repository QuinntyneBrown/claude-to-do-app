using FluentValidation;

namespace Tickbox.Application.Auth.Oidc;

public sealed class CompleteOidcSignInCommandValidator : AbstractValidator<CompleteOidcSignInCommand>
{
    public CompleteOidcSignInCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
    }
}
