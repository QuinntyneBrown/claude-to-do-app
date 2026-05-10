using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tickbox.Application.Auth.RegisterUser;
using Tickbox.Application.Auth.SignInUser;

namespace Tickbox.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterUserCommand(request.Email, request.DisplayName, request.Password), cancellationToken);
        return Ok(result);
    }

    [HttpPost("sign-in")]
    public async Task<ActionResult<SignInUserResult>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SignInUserCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}
