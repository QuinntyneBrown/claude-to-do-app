using MediatR;

namespace Tickbox.Application.Account.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword, string? CallerRefreshToken) : IRequest<Unit>;
