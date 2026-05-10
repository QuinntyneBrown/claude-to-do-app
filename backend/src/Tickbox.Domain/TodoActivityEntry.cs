namespace Tickbox.Domain;

public class TodoActivityEntry
{
    public Guid Id { get; set; }
    public Guid TodoId { get; set; }
    public TodoActivityKind Kind { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
