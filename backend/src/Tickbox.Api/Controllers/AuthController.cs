using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Api.Auth;
using Tickbox.Application.Auth;
using Tickbox.Application.Auth.RefreshAccessToken;
using Tickbox.Application.Auth.PasswordReset;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Auth.SignInUser;
using Tickbox.Application.Auth.SignOut;
using Tickbox.Application.Common;

namespace Tickbox.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public AuthController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _mediator.Send(new RegisterUserCommand(request.Email, request.DisplayName, request.Password), cancellationToken);
        WriteRefreshCookie(outcome);
        return Ok(new RegisterUserResult(outcome.UserId, outcome.AccessToken));
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult<SignInUserResult>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _mediator.Send(new SignInUserCommand(request.Email, request.Password), cancellationToken);
        WriteRefreshCookie(outcome);
        return Ok(new SignInUserResult(outcome.UserId, outcome.AccessToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<SignInUserResult>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(Request);
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new AuthenticationFailedException("Refresh token is invalid.");
        }

        var outcome = await _mediator.Send(new RefreshAccessTokenCommand(refreshToken), cancellationToken);
        WriteRefreshCookie(outcome);
        return Ok(new SignInUserResult(outcome.UserId, outcome.AccessToken));
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(Request);
        await _mediator.Send(new SignOutCommand(refreshToken), cancellationToken);
        RefreshTokenCookie.Clear(Response, secure: !_env.IsDevelopment() && !_env.IsEnvironment("Testing"));
        return NoContent();
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RequestPasswordResetCommand(request.Email), cancellationToken);
        return Accepted();
    }

    [HttpPost("password-reset/complete")]
    [AllowAnonymous]
    public async Task<ActionResult<SignInUserResult>> CompletePasswordReset([FromBody] CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _mediator.Send(new CompletePasswordResetCommand(request.Token, request.NewPassword), cancellationToken);
        WriteRefreshCookie(outcome);
        return Ok(new SignInUserResult(outcome.UserId, outcome.AccessToken));
    }

    private void WriteRefreshCookie(AuthenticationOutcome outcome)
    {
        var secure = !_env.IsDevelopment() && !_env.IsEnvironment("Testing");
        RefreshTokenCookie.Write(Response, outcome.RefreshToken, outcome.RefreshTokenExpiresAt, secure);
    }
}
