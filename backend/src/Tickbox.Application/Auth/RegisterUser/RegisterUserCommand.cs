using MediatR;

namespace Tickbox.Application.Auth.RegisterUser;

public sealed record RegisterUserCommand(string Email, string DisplayName, string Password) : IRequest<AuthenticationOutcome>;
