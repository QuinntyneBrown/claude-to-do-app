namespace Tickbox.Domain;

public class SignInAttempt
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public bool Succeeded { get; set; }
    public string? IpAddress { get; set; }
}
