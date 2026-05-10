using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Api.Auth;
using Tickbox.Application.Account;
using Tickbox.Application.Account.ChangePassword;
using Tickbox.Application.Account.DeleteAccount;
using Tickbox.Application.Account.EmailChange;
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

    [HttpPost("email-change/request")]
    public async Task<ActionResult<MyProfile>> RequestEmailChange([FromBody] EmailChangeRequest request, CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new RequestEmailChangeCommand(request.NewEmail), cancellationToken);
        return Ok(profile);
    }

    [HttpPost("email-change/confirm")]
    public async Task<ActionResult<MyProfile>> ConfirmEmailChange([FromBody] EmailChangeConfirmRequest request, CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new ConfirmEmailChangeCommand(request.Token), cancellationToken);
        return Ok(profile);
    }

    [HttpDelete("email-change")]
    public async Task<ActionResult<MyProfile>> CancelEmailChange(CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new CancelEmailChangeCommand(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var callerRefresh = RefreshTokenCookie.Read(Request);
        await _mediator.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword, callerRefresh), cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMyAccount(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteMyAccountCommand(), cancellationToken);
        return NoContent();
    }
}
