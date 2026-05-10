namespace Tickbox.Infrastructure.Auth;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int LifetimeDays { get; set; } = 14;
}
