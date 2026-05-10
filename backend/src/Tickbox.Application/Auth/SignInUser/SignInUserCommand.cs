using MediatR;

namespace Tickbox.Application.Auth.SignInUser;

public sealed record SignInUserCommand(string Email, string Password) : IRequest<SignInUserResult>;
