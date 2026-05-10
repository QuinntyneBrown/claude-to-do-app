using Microsoft.AspNetCore.Http;
using Tickbox.Application.Common;

namespace Tickbox.Api.Auth;

public sealed class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public RequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? RemoteIp => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var ua = _accessor.HttpContext?.Request.Headers.UserAgent;
            return ua.HasValue ? ua.Value.ToString() : null;
        }
    }
}
