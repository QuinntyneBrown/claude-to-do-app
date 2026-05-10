using Microsoft.AspNetCore.Http;

namespace Tickbox.Api.Auth;

public static class RefreshTokenCookie
{
    public const string Name = "tickbox.refresh";

    private const string CookiePath = "/api/auth";

    public static void Write(HttpResponse response, string plaintext, DateTimeOffset expiresAt, bool secure)
    {
        response.Cookies.Append(Name, plaintext, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expiresAt,
            IsEssential = true
        });
    }

    public static void Clear(HttpResponse response, bool secure)
    {
        response.Cookies.Append(Name, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            MaxAge = TimeSpan.Zero,
            IsEssential = true
        });
    }

    public static string? Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(Name, out var value) ? value : null;
    }
}
