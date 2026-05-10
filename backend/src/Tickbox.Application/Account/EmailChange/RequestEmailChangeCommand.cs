using MediatR;

namespace Tickbox.Application.Account.EmailChange;

public sealed record RequestEmailChangeCommand(string NewEmail) : IRequest<MyProfile>;
