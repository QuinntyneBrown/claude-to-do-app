using MediatR;

namespace Tickbox.Application.Auth.SignOut;

public sealed record SignOutCommand(string? RefreshToken) : IRequest<Unit>;
