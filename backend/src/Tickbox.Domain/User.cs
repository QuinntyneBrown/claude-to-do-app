namespace Tickbox.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? PendingEmail { get; set; }
    public string? PendingEmailTokenHash { get; set; }
    public DateTimeOffset? PendingEmailExpiresAt { get; set; }
}
