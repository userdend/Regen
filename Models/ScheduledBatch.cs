namespace Regen.Models;

public class ScheduledBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int IntervalMilliseconds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ScheduledEntry> Entries { get; set; } = new();
}