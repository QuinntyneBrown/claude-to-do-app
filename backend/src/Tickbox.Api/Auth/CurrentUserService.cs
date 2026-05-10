using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tickbox.Application.Common;

namespace Tickbox.Api.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? _accessor.HttpContext?.User?.FindFirstValue("sub");

            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("No authenticated user is bound to this request.");
        }
    }
}
