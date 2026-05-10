using MediatR;

namespace Tickbox.Application.Account.EmailChange;

public sealed record ConfirmEmailChangeCommand(string Token) : IRequest<MyProfile>;
