using MediatR;

namespace Tickbox.Application.Account.DeleteAccount;

public sealed record DeleteMyAccountCommand() : IRequest<Unit>;
