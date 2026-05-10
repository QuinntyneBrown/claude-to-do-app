namespace Tickbox.Application.Common;

public interface IRequestContext
{
    string? RemoteIp { get; }
    string? UserAgent { get; }
}
