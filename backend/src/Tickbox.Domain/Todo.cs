namespace Tickbox.Domain;

public class Todo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TodoStatus Status { get; set; } = TodoStatus.Incomplete;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
