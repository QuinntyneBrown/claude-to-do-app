using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Application.Account;
using Tickbox.Application.Account.GetMyProfile;
using Tickbox.Application.Account.UpdateDisplayName;

namespace Tickbox.Api.Controllers;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<MyProfile>> GetMyProfile(CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new GetMyProfileQuery(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut("display-name")]
    public async Task<ActionResult<MyProfile>> UpdateDisplayName([FromBody] UpdateDisplayNameRequest request, CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new UpdateDisplayNameCommand(request.DisplayName), cancellationToken);
        return Ok(profile);
    }
}
