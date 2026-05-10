using MediatR;
using Tickbox.Application.Auth;

namespace Tickbox.Application.Auth.PasswordReset;

public sealed record CompletePasswordResetCommand(string Token, string NewPassword) : IRequest<AuthenticationOutcome>;
