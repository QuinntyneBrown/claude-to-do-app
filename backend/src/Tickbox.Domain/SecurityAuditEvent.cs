namespace Tickbox.Domain;

public class SecurityAuditEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public SecurityAuditKind Kind { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Detail { get; set; }
}
