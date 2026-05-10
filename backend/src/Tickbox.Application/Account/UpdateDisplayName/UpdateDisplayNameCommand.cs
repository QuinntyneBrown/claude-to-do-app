using MediatR;

namespace Tickbox.Application.Account.UpdateDisplayName;

public sealed record UpdateDisplayNameCommand(string DisplayName) : IRequest<MyProfile>;
