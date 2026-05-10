using MediatR;

namespace Tickbox.Application.Auth.PasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : IRequest<Unit>;
