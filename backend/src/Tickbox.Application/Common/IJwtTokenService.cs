using Tickbox.Domain;

namespace Tickbox.Application.Common;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
}
