using MediatR;

namespace Tickbox.Application.Account.GetMyProfile;

public sealed record GetMyProfileQuery() : IRequest<MyProfile>;
