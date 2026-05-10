using MediatR;

namespace Tickbox.Application.Account.EmailChange;

public sealed record CancelEmailChangeCommand() : IRequest<MyProfile>;
